using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Contracts.Events
{
    public class StockNotReservedEvent:BaseEvent
    {
        public Guid BuyerId { get; set; }
        public Guid OrderId { get; set; }
        public required string Message { get; set; }
    }
}
