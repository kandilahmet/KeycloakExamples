using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using OrderService.Infrastructure.Cache.Redis;

namespace OrderService.Infrastructure.Security.Auth.OpenIdConnect
{
    public static class OpenIdConnectExtension
    {
        public static AuthenticationBuilder AddCustomOpenIdConnect(
            this AuthenticationBuilder builder,
            IConfiguration configuration
        )
        {
            var settings = configuration
                .GetSection(nameof(OpenIdConnectSettings))
                .Get<OpenIdConnectSettings>();

            return builder.AddOpenIdConnect(
                OpenIdConnectDefaults.AuthenticationScheme,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = settings.Authority;
                    options.ClientId = settings.ClientId;
                    options.ClientSecret = settings.ClientSecret;
                    options.ResponseType = settings.ResponseType;
                    options.SaveTokens = true;
                    //options.UsePkce = true;
                    foreach (var scope in settings.Scopes)
                    {
                        options.Scope.Add(scope);
                    }
                    options.CallbackPath = settings.CallbackPath;
                    options.SignedOutCallbackPath = settings.SignedOutCallbackPath;
                    options.SignedOutRedirectUri = settings.SignedOutRedirectUri;
                    options.AccessDeniedPath = settings.AccessDeniedPath;
                    options.RemoteSignOutPath = settings.RemoteSignOutPath;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = settings.TokenValidationParameter.NameClaimType,
                        RoleClaimType = settings.TokenValidationParameter.RoleClaimType,
                    };

                    options.Events = new OpenIdConnectEvents
                    { //1. calisan kisim
                        OnTokenValidated = async context =>
                        {
                            if (context.TokenEndpointResponse?.AccessToken == null)
                                return;

                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadJwtToken(context.TokenEndpointResponse.AccessToken);

                            var subClaim = context.Principal.FindFirst(ClaimTypes.NameIdentifier)
                                ?? context.Principal.FindFirst("sub");

                            if (string.IsNullOrEmpty(subClaim?.Value))
                                return;

                            // Claims oluştur
                            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

                            // Mevcut claims'leri ekle
                            foreach (var claim in context.Principal.Claims)
                            {
                                identity.AddClaim(claim);
                            }

                            #region RolleriEkle
                            // Client rolleri
                            var resourceAccess = jwtToken.Claims.FirstOrDefault(x => x.Type == "resource_access");
                            if (resourceAccess != null)
                            {
                                var resourceRoles = JObject.Parse(resourceAccess.Value);
                                var clientRoles = resourceRoles[settings.ClientId]?[settings.TokenValidationParameter.RoleClaimType] as JArray;
                                if (clientRoles != null)
                                {
                                    foreach (var role in clientRoles)
                                    {
                                        identity.AddClaim(new Claim(settings.TokenValidationParameter.RoleClaimType, role.ToString()));
                                    }
                                }
                            }

                            // Realm rolleri
                            var realmAccess = jwtToken.Claims.FirstOrDefault(x => x.Type == "realm_access");
                            if (realmAccess != null)
                            {
                                var realmRoles = JObject.Parse(realmAccess.Value);
                                var roles = realmRoles[settings.TokenValidationParameter.RoleClaimType] as JArray;
                                if (roles != null)
                                {
                                    foreach (var role in roles)
                                    {
                                        identity.AddClaim(new Claim(settings.TokenValidationParameter.RoleClaimType, role.ToString()));
                                    }
                                }
                            }
                            #endregion

                            // Yeni principal oluştur
                            var newPrincipal = new ClaimsPrincipal(identity);

                            // Önceki oturumları geçersiz kıl
                            var ticketStore = context.HttpContext.RequestServices.GetRequiredService<RedisTicketStore>();
                            await ticketStore.InvalidateUserSessionsAsync(subClaim.Value);

                            // Principal'ı güncelle
                            context.Principal = newPrincipal;

                            // Properties'i güncelle
                            context.Properties.IsPersistent = true;
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30);
                            context.Properties.AllowRefresh = true;

                            // Token'ları properties'e ekle
                            context.Properties.StoreTokens(new[]
                            {
                                new AuthenticationToken
                                 {
                                     Name = "AccessToken",
                                     Value = context.TokenEndpointResponse.AccessToken,
                                 },
                                 new AuthenticationToken
                                 {
                                     Name = "IdToken",
                                     Value = context.TokenEndpointResponse.IdToken
                                 },
                                 new AuthenticationToken
                                 {
                                     Name = "RefreshToken",
                                     Value = context.TokenEndpointResponse.RefreshToken
                                 }
                             });
                        },

                        OnTicketReceived = async context =>
                        {
                            // Bu event, tüm flow tamamlandığında ve final ticket oluşturulmadan önce çalışır
                            if (context.Principal == null)
                                return;


                            var ticketStore =
                                context.HttpContext.RequestServices.GetRequiredService<RedisTicketStore>();

                            // Ticket'ı Redis'e kaydet
                            var ticket = new AuthenticationTicket(
                                context.Principal,
                                context.Properties,
                                context.Scheme.Name

                            );
                            var key = await ticketStore.StoreAsync(ticket);

                            var cookieSetttings = configuration.GetSection(nameof(CookieSetttings)).Get<CookieSetttings>();

                            // Properties'e key'i ekle
                            context.Properties.Items[cookieSetttings.Name] = key;
                        },
                    };
                }
            );
        }
    }
}
