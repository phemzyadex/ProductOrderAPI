using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.DTOs
{
    public class CreateOrderRequest
    {
        public List<OrderItemRequest> Items { get; set; } = new();

        public CreateOrderRequest() { } 

        public CreateOrderRequest(List<OrderItemRequest> items)
        {
            Items = items;
        }
    }

}
