using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Application.DTOs
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
        public decimal RatePrice { get; set; }
        public decimal Price { get; set; }
        public ProductDto Product { get; set; } = new();
    }
}
