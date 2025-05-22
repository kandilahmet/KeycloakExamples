using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderService.Infrastructure.Cache.Redis;

namespace OrderService.Infrastructure.Security.Auth.Cookies
{
    public static class CookiesExtension
    {
        public static AuthenticationBuilder AddCustomCookies(
            this AuthenticationBuilder builder,
            IConfiguration configuration
        )
        {
            var settings = configuration.GetSection(nameof(CookieSetttings)).Get<CookieSetttings>();

            return builder.AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.SessionStore = builder
                        .Services.BuildServiceProvider()
                        .GetRequiredService<ITicketStore>();

                    options.Cookie.HttpOnly = Convert.ToBoolean(settings.HttpOnly);
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(settings.ExpireTimeSpan);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = settings.Name;
                    options.Cookie.MaxAge = null;
                    options.AccessDeniedPath = settings.AccessDeniedPath;
                    options.LoginPath = settings.LoginPath;

                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningIn = async context =>
                        {
                            // OpenIdConnect'ten gelen authentication'ı kontrol et
                            var authenticationType = context
                                .Principal
                                ?.Identity
                                ?.AuthenticationType;
                            if (authenticationType == OpenIdConnectDefaults.AuthenticationScheme)
                            {
                                // OpenIdConnect flow'undan gelen authentication,
                                // ticket zaten OpenIdConnectExtension'da oluşturuldu
                                return;
                            }

                            var ticketStore =
                                context.HttpContext.RequestServices.GetRequiredService<RedisTicketStore>();

                            // Yeni bir AuthenticationTicket ticket nesnesi oluştur
                            var ticket = new AuthenticationTicket(
                                context.Principal,
                                new AuthenticationProperties
                                {
                                    IsPersistent = true,
                                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(
                                        settings.ExpireTimeSpan
                                    ),
                                    AllowRefresh = true,
                                },
                                context.Scheme.Name
                            );
                           
                            // Yeni oturumu kaydet
                            var key = await ticketStore.StoreAsync(ticket);
                            context.Properties.Items[settings.Name] = key;
                        },

                        OnValidatePrincipal = async context =>
                        {
                            try
                            {
                                var ticketStore =
                                    context.HttpContext.RequestServices.GetRequiredService<RedisTicketStore>();
                                 
                                // Key'i bul
                                if (
                                    !context.Properties.Items.TryGetValue(
                                        settings.Name,
                                        out var keyObj
                                    )
                                    || keyObj == null
                                )
                                {
                                    await HandleInvalidSession(context);
                                    return;
                                }

                                var key = keyObj.ToString();

                                // Ticket'ı kontrol et
                                var ticket = await ticketStore.RetrieveAsync(key);
                                if (ticket == null)
                                {
                                    // Redis'te ticket yoksa, oturumu sonlandır
                                    await HandleInvalidSession(context);
                                    return;
                                }

                                // Süre kontrolü
                                if (
                                    ticket.Properties.ExpiresUtc.HasValue
                                    && ticket.Properties.ExpiresUtc.Value <= DateTimeOffset.UtcNow
                                )
                                {
                                    await HandleInvalidSession(context);
                                    return;
                                }

                                // Principal'ı güncelle
                                context.ReplacePrincipal(ticket.Principal);

                                // Properties'leri güncelle (ama yenileme yapma)
                                context.Properties.Items.Clear(); // Önce mevcut items'ları temizle
                                foreach (var item in ticket.Properties.Items)
                                {
                                    context.Properties.Items[item.Key] = item.Value;
                                }

                                // Önemli: ShouldRenew false olmalı, yoksa yeni ticket oluşturur
                                context.ShouldRenew = false;
                            }
                            catch
                            {
                                await HandleInvalidSession(context);
                            }
                        },

                        OnSigningOut = async context =>
                        {
                            try
                            {
                                var ticketStore =
                                    context.HttpContext.RequestServices.GetRequiredService<RedisTicketStore>();

                                if (
                                    context.Properties.Items.TryGetValue(
                                        settings.Name,
                                        out var keyObj
                                    )
                                    && keyObj != null
                                )
                                {
                                    await ticketStore.RemoveAsync(keyObj.ToString());
                                }

                                //// Kullanıcının tüm oturumlarını sonlandır (isteğe bağlı)
                                //var userName = context
                                //    .HttpContext.User.Claims.FirstOrDefault(x =>
                                //        x.Type == settings.TokenValidationParameter.NameClaimType
                                //    )
                                //    ?.Value;

                                //if (!string.IsNullOrEmpty(userName))
                                //{
                                // await ticketStore.InvalidateUserSessionsAsync(userName);
                                //}
                            }
                            catch
                            {
                                // Hata durumunda sessizce devam et
                            }
                        },
                    };
                }
            );
        }

        private static async Task HandleInvalidSession(CookieValidatePrincipalContext context)
        {
            // Principal'ı reddet
            context.RejectPrincipal();

            // Cookie'yi hemen sil
            var cookieName = context.Options.Cookie.Name;
            context.HttpContext.Response.Cookies.Delete(
                cookieName,
                new CookieOptions
                {
                    Path = "/",
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                }
            );

            // Authentication'ı temizle
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            // Kullanıcıyı login sayfasına yönlendir
            context.HttpContext.Response.Redirect(context.Options.LoginPath);
        }
    }
}
