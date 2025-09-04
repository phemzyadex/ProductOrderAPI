using Microsoft.Extensions.Logging;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;
    private readonly IAuditService _auditService;

    public OrderService(AppDbContext db, ILogger<OrderService> logger, IAuditService auditService)
    {
        _db = db;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<Order> PlaceOrderAsync(CreateOrderRequest request, Guid userId)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId, 
                Quantity = i.Quantity
            }).ToList()
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in order.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null)
                throw new KeyNotFoundException($"Product {item.ProductId} not found.");

            if (product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Not enough stock for product {product.Name}");

            product.StockQuantity -= item.Quantity;
            _db.Products.Update(product);

            item.Price = product.Price;
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditService.LogEventAsync("OrderPlaced", userId.ToString(),
            $"Order {order.Id} placed with {order.Items.Count} items");

        return order;
    }
}
