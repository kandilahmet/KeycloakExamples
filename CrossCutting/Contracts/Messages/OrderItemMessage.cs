using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.Contracts.Messages
{
    public class OrderItemMessage
    {
        public required string ProductId { get; set; }
        public int Count { get; set; }
    }
}
