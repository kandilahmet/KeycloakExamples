using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace OrderService.Core.Application.Features.Commands.Create
{
    public class CreateOrderCommandRequest : IRequest<bool>
    {
        public Guid BuyerId { get; set; }
        public required List<CreateOrderItemCommandRequest> CreateOrderItems { get; set; } 
    }
}
