using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Features.Queries.Response
{
    public class GetAllOrdersQueryResponse
    {
        public List<GetOrderQueryResponse> getOrderQueryResponses { get; set; }
    }
}
