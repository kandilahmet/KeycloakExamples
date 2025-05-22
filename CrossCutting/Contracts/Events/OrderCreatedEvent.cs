using CrossCutting.Contracts.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Contracts.Events
{
    public class OrderCreatedEvent: BaseEvent
    {  
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public required List<OrderItemMessage> OrderItems { get; set; } 
        public decimal TotalPrice { get; set; }
    }
}
