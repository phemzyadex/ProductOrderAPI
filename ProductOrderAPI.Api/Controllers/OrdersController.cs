using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly AppDbContext _db;

    public OrderController(IOrderService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return Unauthorized(ApiResponse<string>.Fail(null, "User does not exist"));

        //var order = new Order
        //{
        //    Id = Guid.NewGuid(),
        //    Items = request.Items.Select(i => new OrderItem
        //    {
        //        ProductId = i.ProductId,
        //        Quantity = i.Quantity
        //    }).ToList()
        //};

        var result = await _service.PlaceOrderAsync(request, user.Id);

        return Ok(ApiResponse<object>.Ok(new { OrderId = result.Id }, "Order created successfully"));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(ApiResponse<string>.Fail(null, "Order not found"));

        if (!await CanAccessOrderAsync(order))
            return Forbid();

        return Ok(ApiResponse<OrderDto>.Ok(MapToDto(order)));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        string? username = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var ordersQuery = _db.Orders
            .Include(o => o.Items).ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .AsQueryable();

        var currentUsername = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role != "Admin")
        {
            ordersQuery = ordersQuery.Where(o => o.User.Username == currentUsername);
        }
        else if (!string.IsNullOrEmpty(username))
        {
            ordersQuery = ordersQuery.Where(o => o.User.Username == username);
        }

        if (startDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value);

        ordersQuery = ordersQuery.OrderByDescending(o => o.OrderDate);

        var totalOrders = await ordersQuery.CountAsync();
        var orders = await ordersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var ordersDto = orders.Select(MapToDto).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalOrders = totalOrders,
            Page = page,
            PageSize = pageSize,
            Orders = ordersDto
        }));
    }

    #region Helpers

    private async Task<User?> GetCurrentUserAsync()
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(username))
            return null;

        return await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
    }

    private async Task<bool> CanAccessOrderAsync(Order order)
    {
        var username = User.FindFirstValue(ClaimTypes.Name);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return order.User.Username == username || role == "Admin";
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CreatedDate = order.OrderDate,
            TotalAmount = order.Items.Sum(i => i.Price * i.Quantity),
            Items = order.Items.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                Quantity = oi.Quantity,
                RatePrice = oi.Price,
                Price = oi.Price * oi.Quantity,
                Product = new ProductDto
                {
                    Id = oi.Product.Id,
                    Name = oi.Product.Name,
                    Description = oi.Product.Description,
                    Price = oi.Product.Price
                }
            }).ToList()
        };
    }

    #endregion
}
