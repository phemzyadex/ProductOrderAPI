using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductOrderAPI.Tests
{
    public static class SeedTestData
    {
        public static void SeedProducts(AppDbContext context)
        {
            context.Products.AddRange(
                new Product { Id = Guid.NewGuid(), Name = "Laptop", Price = 1200, Description = "High-performance laptop", StockQuantity = 10},
                new Product { Id = Guid.NewGuid(), Name = "Phone", Price = 800, Description = "Latest smartphone", StockQuantity = 20 }
            );
            context.SaveChanges();
        }
    }
}
