using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CrossCutting.Contracts.Events;
using System.Text.Json;
using MassTransit;
using MassTransit.Transports;
using Newtonsoft.Json;
using System.Net.Http.Json; 
using OrderService.Infrastructure.Cache.Redis;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Text.RegularExpressions;

namespace OrderService.Infrastructure.MessageBroker.RabbitMQ.Consumers
{
    public class TokenChangedEventConsumer : IConsumer<JsonObject>
    {
        private readonly RedisTicketStore _redisTicketStore;

        public TokenChangedEventConsumer(RedisTicketStore redisTicketStore)
        {
            _redisTicketStore = redisTicketStore;
        }
        public async Task Consume(ConsumeContext<JsonObject> context)
        {
            try
            {

                var message = context.Message.ToString();
                var tokenChangedEvent = JsonConvert.DeserializeObject<TokenChangedEvent>(message);
                //var tokenChangedEvent2 = System.Text.Json.JsonSerializer.Deserialize<TokenChangedEvent>(message);  Newtonsoft.Json daha esnek ve hata toleransı daha yüksek.

                if (message == null)
                {

                    throw new ArgumentNullException(nameof(message), "Message is null.");
                }
               

                // Regex ile userId ve groupId çıkar
                var match = Regex.Match(tokenChangedEvent.ResourcePath, @"users\/([^\/]+)\/groups\/([^\/]+)");

                if (!match.Success)
                    return;

                string userId = match.Groups[1].Value;
                string groupId = match.Groups[2].Value;

                // Önceki oturumları geçersiz kıl
                await _redisTicketStore.InvalidateUserSessionsAsync(userId);

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                throw;
            }
        }
    }
}
