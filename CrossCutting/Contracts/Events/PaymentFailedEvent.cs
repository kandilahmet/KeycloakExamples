using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Contracts.Events
{
    public class PaymentFailedEvent:BaseEvent
    {
        public Guid OrderId { get; set; }
        public required string Message { get; set; }
    }
}
