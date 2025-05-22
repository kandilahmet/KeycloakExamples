using CrossCutting.Abstractions;
using CrossCutting.Configuration;
using CrossCutting.Contracts;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Cache;
using OrderService.Infrastructure.Cache.Redis;
using OrderService.Infrastructure.MessageBroker.RabbitMQ;
using OrderService.Infrastructure.MessageBroker.RabbitMQ.Consumers;
using StackExchange.Redis;
using System.Runtime;
using System.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using OrderService.Infrastructure.Security.Auth.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
namespace OrderService.Infrastructure.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureService(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {            
            services.AddRabbitMQ(configuration);
            services.AddSingleton(new RedisDbContext(configuration.GetSection("Redis").Value));
            services.AddSingleton<ICacheService, RedisService>();
            services.AddSingleton<ITicketStore, RedisTicketStore>();
            services.AddSingleton<RedisTicketStore, RedisTicketStore>();
            //services.Configure<OpenIdConnectSettings>(configuration.GetSection(nameof(OpenIdConnectSettings)));
            //services.Configure<CookieSetttings>(configuration.GetSection(nameof(CookieSetttings)));
            //services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectConfigurer>();

        }

      
    }
}
