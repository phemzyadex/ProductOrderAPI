using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using System.Security.Claims;
using Xunit;

namespace ProductOrderAPI.Tests.Controllers
{
    public class OrderControllerTests
    {
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public OrderControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated per test
                .Options;
        }

        private static ClaimsPrincipal CreateUser(string username, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        }

        private OrderController CreateController(
            AppDbContext db,
            Mock<IOrderService> orderServiceMock,
            ClaimsPrincipal? user = null)
        {
            var controller = new OrderController(orderServiceMock.Object, db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = user ?? CreateUser("testuser")
                    }
                }
            };

            return controller;
        }

        [Theory]
        [InlineData("User", "testuser", "testuser", true)]   // User should see own orders
        [InlineData("Admin", "admin", "john", true)]         // Admin can filter by username
        [InlineData("User", "testuser", "john", true)] // User sees john's orders if filter is used
        [InlineData("User", "testuser", "john", false)]      // User should NOT see others’ orders

        public async Task GetOrders_ShouldRespectRoleAndUsername(
    string role,
    string currentUsername,
    string? filterUsername,
    bool shouldSeeOrders)
        {
            using var db = new AppDbContext(_dbOptions);

            // Arrange users
            var user1 = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "hash", Role = "User" };
            var user2 = new User { Id = Guid.NewGuid(), Username = "john", PasswordHash = "hash", Role = "User" };
            db.Users.AddRange(user1, user2);

            // Add orders for both users
            db.Orders.Add(new Order { Id = Guid.NewGuid(), User = user1, UserId = user1.Id, OrderDate = DateTime.UtcNow });
            db.Orders.Add(new Order { Id = Guid.NewGuid(), User = user2, UserId = user2.Id, OrderDate = DateTime.UtcNow });
            db.SaveChanges();

            var mockService = new Mock<IOrderService>();
            var controller = CreateController(db, mockService, CreateUser(currentUsername, role));

            // Act
            var result = await controller.GetOrders(username: filterUsername);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);

            var data = response.Data!.GetType().GetProperty("Orders")!.GetValue(response.Data);
            var orders = (IEnumerable<OrderDto>)data!;

            if (shouldSeeOrders)
                Assert.NotEmpty(orders);
            else
                Assert.Empty(orders);
        }


        [Fact]
        public async Task PlaceOrder_ShouldReturnOk_WhenUserExists()
        {
            using var db = new AppDbContext(_dbOptions);
            var user = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "hash", Role = "User" };
            db.Users.Add(user);
            db.SaveChanges();

            var mockService = new Mock<IOrderService>();
            var fakeOrder = new Order { Id = Guid.NewGuid(), UserId = user.Id };
            mockService.Setup(s => s.PlaceOrderAsync(It.IsAny<CreateOrderRequest>(), user.Id))
                       .ReturnsAsync(fakeOrder);

            var controller = CreateController(db, mockService);
            var request = new CreateOrderRequest { Items = new List<OrderItemRequest> { new(Guid.NewGuid(), 1) } };

            var result = await controller.PlaceOrder(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task PlaceOrder_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            using var db = new AppDbContext(_dbOptions);
            var mockService = new Mock<IOrderService>();
            var controller = CreateController(db, mockService);

            var result = await controller.PlaceOrder(new CreateOrderRequest());

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ------------------------
        // GetOrderById
        // ------------------------
        [Fact]
        public async Task GetOrderById_ShouldReturnNotFound_WhenOrderDoesNotExist()
        {
            using var db = new AppDbContext(_dbOptions);
            var controller = CreateController(db, new Mock<IOrderService>());

            var result = await controller.GetOrderById(Guid.NewGuid());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnForbid_WhenUserTriesToAccessOthersOrder()
        {
            using var db = new AppDbContext(_dbOptions);

            var owner = new User { Id = Guid.NewGuid(), Username = "john", PasswordHash = "hash", Role = "User" };
            var intruder = new User { Id = Guid.NewGuid(), Username = "peter", PasswordHash = "hash", Role = "User" };
            db.Users.AddRange(owner, intruder);

            db.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = owner.Id, User = owner, OrderDate = DateTime.UtcNow });
            db.SaveChanges();

            var controller = CreateController(db, new Mock<IOrderService>(), CreateUser("peter"));

            var result = await controller.GetOrderById(db.Orders.First().Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnOrder_WhenUserOwnsOrder()
        {
            using var db = new AppDbContext(_dbOptions);

            var user = new User { Id = Guid.NewGuid(), Username = "john", PasswordHash = "hash", Role = "User" };
            var product = new Product { Id = Guid.NewGuid(), Name = "Laptop", Description = "Gaming", Price = 1000, StockQuantity = 5 };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                OrderDate = DateTime.UtcNow,
                Items = new List<OrderItem> { new() { Id = Guid.NewGuid(), ProductId = product.Id, Product = product, Quantity = 2, Price = product.Price } }
            };

            db.Users.Add(user);
            db.Products.Add(product);
            db.Orders.Add(order);
            db.SaveChanges();

            var controller = CreateController(db, new Mock<IOrderService>(), CreateUser("john"));

            var result = await controller.GetOrderById(order.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<OrderDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(order.Id, response.Data.Id);
            Assert.Equal(2, response.Data.Items.First().Quantity);
        }

        // ------------------------
        // GetOrders
        // ------------------------
        [Fact]
        public async Task GetOrders_ShouldFilterByCurrentUser_WhenNotAdmin()
        {
            using var db = new AppDbContext(_dbOptions);
            var user = new User { Id = Guid.NewGuid(), Username = "testuser", PasswordHash = "hash", Role = "User" };
            db.Users.Add(user);
            db.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = user.Id, User = user, OrderDate = DateTime.UtcNow });
            db.SaveChanges();

            var controller = CreateController(db, new Mock<IOrderService>(), CreateUser("testuser", "User"));

            var result = await controller.GetOrders();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetOrders_ShouldAllowAdminToFilterByUsername()
        {
            using var db = new AppDbContext(_dbOptions);
            var user = new User { Id = Guid.NewGuid(), Username = "john", PasswordHash = "hash", Role = "User" };
            db.Users.Add(user);
            db.Orders.Add(new Order { Id = Guid.NewGuid(), UserId = user.Id, User = user, OrderDate = DateTime.UtcNow });
            db.SaveChanges();

            var controller = CreateController(db, new Mock<IOrderService>(), CreateUser("admin", "Admin"));

            var result = await controller.GetOrders(username: "john");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }
    }
}
