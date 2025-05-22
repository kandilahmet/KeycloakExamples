using Application.Abstractions.Repositories.EFCore;
using CrossCutting.Contracts.Events;
using MassTransit;
using OrderService.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.MessageBroker.RabbitMQ.Consumers
{
    public class PaymentFailedEventConsumer(IEFCoreWriteRepository<Order> eFCoreWriteRepository, IEFCoreReadRepository<Order> eFCoreReadRepository) : IConsumer<PaymentFailedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            Order order = await eFCoreReadRepository.GetSingleAsync(x => x.ID == context.Message.OrderId);
            order.OrderStatus = OrderStatus.Failed;
            await eFCoreWriteRepository.SaveAsync();
        }
    }
}
