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
    public async Task<ApiResponse<Order>> PlaceOrderAsync(CreateOrderRequest request, Guid userId)
    {
        if (request.Items == null || !request.Items.Any())
            return ApiResponse<Order>.Fail("Order must contain at least one item.", "false");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return ApiResponse<Order>.Fail( $"Invalid quantity for product {item.ProductId}.", "false");

                
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product == null)
                    return ApiResponse<Order>.Fail($"Product {item.ProductId} not found.", "false");

                if (product.StockQuantity < item.Quantity)
                    return ApiResponse<Order>.Fail($"Not enough stock for product {product.Name}.", "false");

                // Deduct stock
                product.StockQuantity -= item.Quantity;
                _db.Products.Update(product);

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogEventAsync("OrderPlaced", userId.ToString(),
                $"Order {order.Id} placed with {order.Items.Count} items");

            _logger.LogInformation("Order {OrderId} placed successfully by User {UserId}", order.Id, userId);

            return ApiResponse<Order>.Ok(order, "Order created successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error placing order for User {UserId}", userId);
            return ApiResponse<Order>.Fail( "An error occurred while placing the order.", "false");
        }
    }

}
