using Application.Abstractions.Repositories.EFCore;
using CrossCutting.Contracts.Events;
using OrderService.Core.Domain.Entities;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.MessageBroker.RabbitMQ.Consumers
{
    public class PaymentCompletedEventConsumer(IEFCoreWriteRepository<Order> eFCoreWriteRepository, IEFCoreReadRepository<Order> eFCoreReadRepository) : IConsumer<PaymentCompletedEvent>
    {
        public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
        {
            Order order = await eFCoreReadRepository.GetSingleAsync(x => x.ID == context.Message.OrderId);
            order.OrderStatus = OrderStatus.Completed;
            await eFCoreWriteRepository.SaveAsync();
        }
    }
}
