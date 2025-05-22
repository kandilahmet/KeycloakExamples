using CrossCutting.Abstractions;
using CrossCutting.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Cache.Redis;
using OrderService.Infrastructure.MessageBroker.RabbitMQ.Consumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.MessageBroker.RabbitMQ
{
    public static class RabbitMQRegistration
    {
        public static void AddRabbitMQ(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            var rabbitMqUrl = configuration.GetSection("RabbitMQ").Value;
            if (string.IsNullOrEmpty(rabbitMqUrl))
                throw new ArgumentNullException(
                    nameof(rabbitMqUrl),
                    "RabbitMQ connection string is not configured."
                );

            services.AddMassTransit(configurator =>
            {
                configurator.AddConsumer<StockNotReservedEventConsumer>();
                configurator.AddConsumer<PaymentCompletedEventConsumer>();
                configurator.AddConsumer<PaymentFailedEventConsumer>();
                configurator.AddConsumer<TokenChangedEventConsumer>();

                configurator.UsingRabbitMq(
                    (context, config) =>
                    {
                        config.Host(rabbitMqUrl);

                        // Default message retry policy
                        config.UseMessageRetry(retry =>
                        {
                            retry.Intervals(
                                TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(5),
                                TimeSpan.FromSeconds(10)
                            );
                        });

                        // Configure standard event consumers
                        ConfigureStandardEventConsumer<StockNotReservedEventConsumer>(
                            config,
                            context,
                            RabbitMQSettings.Order_StockNotReservedEventQueue
                        );
                        ConfigureStandardEventConsumer<PaymentCompletedEventConsumer>(
                            config,
                            context,
                            RabbitMQSettings.Order_PaymentCompletedEventQueue
                        );
                        ConfigureStandardEventConsumer<PaymentFailedEventConsumer>(
                            config,
                            context,
                            RabbitMQSettings.Order_PaymentFailedEventQueue
                        );

                        // TokenChangedEventConsumer için özel yapılandırma
                        config.ReceiveEndpoint(
                            RabbitMQSettings.TokenChangeEventQueue,
                            e =>
                            {
                                e.ClearSerialization();
                                e.UseRawJsonSerializer();
                                e.Bind(
                                    "keycloak_events",
                                    x =>
                                    {
                                        x.ExchangeType = "topic";
                                        x.RoutingKey = "user.*";
                                        x.Durable = true;
                                        x.AutoDelete = false;
                                    }
                                );

                                e.ConfigureConsumer<TokenChangedEventConsumer>(context);
                            }
                        );
                    }
                );
            });

           
        }

        private static void ConfigureStandardEventConsumer<TConsumer>(
            IRabbitMqBusFactoryConfigurator config,
            IBusRegistrationContext context,
            string queueName
        )
            where TConsumer : class, IConsumer
        {
            config.ReceiveEndpoint(
                queueName,
                e =>
                {
                    e.ConfigureConsumer<TConsumer>(context);
                }
            );
        }
    }
}
