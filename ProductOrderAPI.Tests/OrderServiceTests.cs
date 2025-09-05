using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Tests.Helper;
using Moq;
using Microsoft.Extensions.Logging;
using ProductOrderAPI.Application.Interfaces;

namespace ProductOrderAPI.Tests
{
    public class OrderServiceTests
    {
        private readonly Guid testUserId = Guid.NewGuid();

        [Fact]
        public async Task PlaceOrderAsync_ShouldReduceStock_WhenOrderIsValid()
        {
            // Arrange
            var context = DbContextHelper.GetInMemoryDbContext();
            SeedTestData.SeedProducts(context);

            var loggerMock = new Mock<ILogger<OrderService>>();
            var auditServiceMock = new Mock<IAuditService>();

            var service = new OrderService(context, loggerMock.Object, auditServiceMock.Object);

            var orderRequest = new CreateOrderRequest
            {
                Items = new List<OrderItemRequest>
                {

                    new OrderItemRequest(context.Products.First().Id, 2)
                }
            };

            // Act
            await service.PlaceOrderAsync(orderRequest, testUserId);

            // Assert
            var product = context.Products.First();
            Assert.Equal(8, product.StockQuantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_ShouldThrow_WhenNotEnoughStock()
        {
            // Arrange
            var context = DbContextHelper.GetInMemoryDbContext();
            SeedTestData.SeedProducts(context);

            var loggerMock = new Mock<ILogger<OrderService>>();
            var auditServiceMock = new Mock<IAuditService>();

            var service = new OrderService(context, loggerMock.Object, auditServiceMock.Object);

            var orderRequest = new CreateOrderRequest
            {
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest(context.Products.First().Id, 1000)
                }
            };

            // Act
            await service.PlaceOrderAsync(orderRequest, testUserId);

            // Assert
            Assert.Equal($"Not enough stock for product {context.Products.First().Name}.", $"Not enough stock for product {context.Products.First().Name}.");
                
        }

        [Fact]
        public async Task PlaceOrderAsync_ShouldThrow_WhenProductDoesNotExist()
        {
            // Arrange
            var context = DbContextHelper.GetInMemoryDbContext();
            SeedTestData.SeedProducts(context);

            var loggerMock = new Mock<ILogger<OrderService>>();
            var auditServiceMock = new Mock<IAuditService>();

            var service = new OrderService(context, loggerMock.Object, auditServiceMock.Object);

            var invalidProductId = Guid.NewGuid();
            var orderRequest = new CreateOrderRequest
            {
                Items = new List<OrderItemRequest>
                {
                    new OrderItemRequest(invalidProductId, 1)
                }
            };


            // Act
            await service.PlaceOrderAsync(orderRequest, testUserId);

            // Assert
            Assert.Equal($"Product {invalidProductId} not found.", $"Product {invalidProductId} not found.");
        }
    }
}
