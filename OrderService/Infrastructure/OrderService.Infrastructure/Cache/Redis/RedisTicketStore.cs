using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace OrderService.Infrastructure.Cache.Redis
{
    public class RedisTicketStore : ITicketStore
    {
        private const string KeyPrefix = "auth:";
        private const string UserPrefix = "user:";
        private readonly IDatabase _db;
        private readonly IConfiguration _configuration;

        public RedisTicketStore(RedisDbContext redisDbContext, IConfiguration configuration)
        {
            _db = redisDbContext.GetDatabase(0);
            _configuration = configuration;
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                // Önce ticket'ı al
                var ticket = await RetrieveAsync(key);

                if (ticket != null)
                {
                    var subClaim = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)
                     ?? ticket.Principal.FindFirst("sub"); // Fallback for OIDC
                     

                    if (!string.IsNullOrEmpty(subClaim.Value))
                    {
                        // Kullanıcının aktif oturum listesinden bu oturumu kaldır
                        var userSessionKey = $"{UserPrefix}{subClaim.Value}";
                        await _db.SetRemoveAsync(userSessionKey, key);

                        // Eğer kullanıcının başka aktif oturumu kalmadıysa user key'ini sil
                        var remainingSessions = await _db.SetLengthAsync(userSessionKey);
                        if (remainingSessions == 0)
                        {
                            await _db.KeyDeleteAsync(userSessionKey);
                        }
                    }
                }

                // Ticket'ı sil
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception)
            {
                // Hata durumunda sessizce devam et
            }
        }

        public async Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            if (string.IsNullOrEmpty(key) || ticket == null)
                return;

            var data = TicketSerializer.Default.Serialize(ticket);
            var expiresIn =
                ticket.Properties.ExpiresUtc?.Subtract(DateTimeOffset.UtcNow)
                ?? TimeSpan.FromMinutes(30);

            await _db.StringSetAsync(key, data, expiresIn);
        }

        public async Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var data = await _db.StringGetAsync(key);
            if (data.IsNullOrEmpty)
                return null;

            try
            {
                var ticket = TicketSerializer.Default.Deserialize(data);
                if (ticket == null)
                    return null;

                // Ticket süresini kontrol et
                if (
                    ticket.Properties.ExpiresUtc.HasValue
                    && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow
                )
                {
                    await RemoveAsync(key);
                    return null;
                }

                return ticket;
            }
            catch
            {
                await RemoveAsync(key);
                return null;
            }
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {

           
            var subClaim = ticket.Principal.FindFirst(ClaimTypes.NameIdentifier)
                  ?? ticket.Principal.FindFirst("sub"); 

             

            var userId = subClaim.Value;

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("User userId claim not found in the ticket.");

            
            var sessionId = $"{KeyPrefix}{Guid.NewGuid():N}";

            // Kullanıcının aktif oturumlarını tutan set'e bu oturumu ekle
            var userSessionKey = $"{UserPrefix}{userId}";

            var data = TicketSerializer.Default.Serialize(ticket);
            var expiresIn =
                ticket.Properties.ExpiresUtc?.Subtract(DateTimeOffset.UtcNow)
                ?? TimeSpan.FromMinutes(30);

            // Transaction içinde işlemleri gerçekleştir
            var tran = _db.CreateTransaction();

            // Oturum bilgisini kaydet
            tran.StringSetAsync(sessionId, data, expiresIn);

            // Kullanıcının oturumlar listesine ekle
            tran.SetAddAsync(userSessionKey, sessionId);

            // Kullanıcının oturumlar listesi için de aynı expire time'ı ayarla
            tran.KeyExpireAsync(userSessionKey, expiresIn);

            // Transaction'ı çalıştır
            var success = await tran.ExecuteAsync();
            if (!success)
                throw new InvalidOperationException("Failed to store authentication ticket.");

            return sessionId;
        }

        public async Task InvalidateUserSessionsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var userSessionKey = $"{UserPrefix}{key}";

            // Kullanıcının tüm oturumlarını al
            var sessionIds = await _db.SetMembersAsync(userSessionKey);

            if (sessionIds.Any())
            {
                var tran = _db.CreateTransaction();

                // Tüm oturum bilgilerini sil
                foreach (var sessionId in sessionIds)
                {
                    tran.KeyDeleteAsync(sessionId.ToString());
                }

                // Kullanıcının oturumlar listesini sil
                tran.KeyDeleteAsync(userSessionKey);

                await tran.ExecuteAsync();
            }
        }
    }
}
