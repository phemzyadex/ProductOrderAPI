using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using ProductOrderAPI.Infrastructure.Services;
using System.Security.Claims;
using Xunit;

namespace ProductOrderAPI.Tests
{
    public class ProductServiceTests
    {
        private static AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static IHttpContextAccessor GetHttpContextAccessor(string username = "testuser")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(x => x.HttpContext).Returns(httpContext);
            return accessor.Object;
        }

        [Fact]
        public async Task CreateAsync_ShouldAddProduct_AndLogEvent()
        {
            // Arrange
            var db = GetDbContext();
            var logger = Mock.Of<ILogger<ProductService>>();
            var auditMock = new Mock<IAuditService>();
            var httpAccessor = GetHttpContextAccessor("alice");

            var service = new ProductService(db, logger, auditMock.Object, httpAccessor);

            var dto = new ProductRequestDto
            {
                Name = "Laptop",
                Description = "High-end gaming laptop",
                Price = 1500,
                StockQuantity = 5
            };

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Laptop", result.Name);
            Assert.Equal(5, result.StockQuantity);

            auditMock.Verify(a => a.LogEventAsync(
                "ProductCreated",
                "alice",
                It.Is<string>(msg => msg.Contains("Laptop"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrow_WhenRowVersionMismatch()
        {
            // Arrange
            var db = GetDbContext();
            var logger = Mock.Of<ILogger<ProductService>>();
            var auditMock = new Mock<IAuditService>();
            var httpAccessor = GetHttpContextAccessor();

            var service = new ProductService(db, logger, auditMock.Object, httpAccessor);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Description = "Latest model",
                Price = 800,
                StockQuantity = 10,
                RowVersion = new byte[] { 1, 2, 3 }
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var dto = new ProductDto
            {
                Id = product.Id,
                Name = "Phone X",
                Description = "Updated model",
                Price = 900,
                StockQuantity = 15,
                RowVersion = new byte[] { 9, 9, 9 } // wrong version
            };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                service.UpdateAsync(product.Id, dto));
        }

        [Fact]
        public async Task AddStockAsync_ShouldIncreaseQuantity_AndLogEvent()
        {
            // Arrange
            var db = GetDbContext();
            var logger = Mock.Of<ILogger<ProductService>>();
            var auditMock = new Mock<IAuditService>();
            var httpAccessor = GetHttpContextAccessor("bob");

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Tablet",
                Description = "Android tablet",
                Price = 400,
                StockQuantity = 3
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var service = new ProductService(db, logger, auditMock.Object, httpAccessor);

            // Act
            var result = await service.AddStockAsync(product.Id, 2);

            // Assert
            Assert.Equal(5, result.StockQuantity);

            auditMock.Verify(a => a.LogEventAsync(
                "StockUpdated",
                "bob",
                It.Is<string>(msg => msg.Contains("NewStock: 5"))),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveProduct_AndLogEvent()
        {
            // Arrange
            var db = GetDbContext();
            var logger = Mock.Of<ILogger<ProductService>>();
            var auditMock = new Mock<IAuditService>();
            var httpAccessor = GetHttpContextAccessor("charlie");

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Monitor",
                Description = "4K UHD Monitor",
                Price = 300,
                StockQuantity = 7
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            var service = new ProductService(db, logger, auditMock.Object, httpAccessor);

            // Act
            await service.DeleteAsync(product.Id);

            // Assert
            var deleted = await db.Products.FindAsync(product.Id);
            Assert.Null(deleted);

            auditMock.Verify(a => a.LogEventAsync(
                "ProductDeleted",
                "charlie",
                It.Is<string>(msg => msg.Contains(product.Id.ToString()))),
                Times.Once);
        }

    }
}
