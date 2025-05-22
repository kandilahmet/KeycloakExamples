using System;
using System.Collections.Generic;
using Application.Abstractions.Repositories.EFCore;
using CrossCutting.Contracts.Events;
using CrossCutting.Contracts.Messages;
using MassTransit;
using MediatR;
using OrderService.Core.Domain.Entities;

namespace OrderService.Core.Application.Features.Commands.Create
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommandRequest, bool>
    {
        private readonly IEFCoreWriteRepository<Order> _writeOrderRepository;
        private readonly IPublishEndpoint _publisher;

        public CreateOrderCommandHandler(
            IEFCoreWriteRepository<Order> writeOrderRepository,
            IPublishEndpoint publisher
        )
        {
            _writeOrderRepository = writeOrderRepository;
            _publisher = publisher;
        }

        public async Task<bool> Handle(
            CreateOrderCommandRequest request,
            CancellationToken cancellationToken
        )
        {
            Guid _orderId = Guid.NewGuid();
            var order = new Order()
            {
                BuyerId = request.BuyerId,
                CreatedDate = DateTime.UtcNow,
                ID = _orderId,
                TotalPrice = request.CreateOrderItems.Sum(x => x.Count * x.Price),
                OrderItems = request
                    .CreateOrderItems.Select(oi => new OrderItem()
                    {
                        Count = oi.Count,
                        Price = oi.Price,
                        ID = Guid.NewGuid(),
                        OrderId = _orderId,
                        ProductId = oi.ProductId,
                    })
                    .ToList(),
            };

            await _writeOrderRepository.AddAsync(order);
            bool result = await _writeOrderRepository.SaveAsync();

            if (result)
            {
                var orderCreatedEvent = new OrderCreatedEvent()
                {
                    BuyerId = order.BuyerId,
                    OrderId = order.ID,
                    OrderItems = order
                        .OrderItems.Select(oi => new OrderItemMessage()
                        {
                            Count = oi.Count,
                            ProductId = oi.ProductId.ToString(),
                        })
                        .ToList(),
                    TotalPrice = order.TotalPrice,
                };
                await _publisher.Publish(orderCreatedEvent);
            }

            return true;
        }
    }
}
