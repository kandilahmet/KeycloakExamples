using Application.Abstractions.Repositories.EFCore;
using MediatR;
using OrderService.Application.Features.Queries.Request;
using OrderService.Application.Features.Queries.Response;
using OrderService.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Features.Queries.Handler
{
    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQueryRequest, GetAllOrdersQueryResponse>
    {
        private readonly IEFCoreReadRepository<Order> _readOrderRepository;
        public GetAllOrdersQueryHandler(IEFCoreReadRepository<Order> readOrderRepository)
        {
            _readOrderRepository = readOrderRepository;
        }
        public async Task<GetAllOrdersQueryResponse> Handle(GetAllOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var orders = await _readOrderRepository.GetAll();

            var response = new GetAllOrdersQueryResponse
            {
                getOrderQueryResponses = orders.Select(x => new GetOrderQueryResponse
                {
                    BuyerId = x.BuyerId,
                    CreatedDate = x.CreatedDate,
                    OrderId = x.ID,
                    OrderStatus = x.OrderStatus,
                    TotalPrice = x.TotalPrice,
                    OrderItems = x.OrderItems.Select(y => new GetOrderItemQueryResponse
                    {
                        Count = y.Count,
                        OrderItemId = y.ID,
                        Price = y.Price,
                        ProductId = y.ProductId,
                    }).ToList()
                }).ToList()
            };

            return response;


        }
    }
}
