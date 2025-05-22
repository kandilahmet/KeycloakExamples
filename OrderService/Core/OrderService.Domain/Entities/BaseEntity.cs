using CrossCutting.Abstractions.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Core.Domain.Entities
{
    public class BaseEntity:IBaseEntity
    {
        public Guid ID { get; set; }
    }
}
