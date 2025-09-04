using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductOrderAPI.Api.Controllers;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using System.Security.Claims;
using Xunit;

namespace ProductOrderAPI.Tests.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductService> _mockService;

        public ProductControllerTests()
        {
            _mockService = new Mock<IProductService>();
        }

        private static ProductController CreateController(
            Mock<IProductService> mockService,
            string? role = null)
        {
            var controller = new ProductController(mockService.Object);

            if (role != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.Role, role) }, "mock"))
                    }
                };
            }

            return controller;
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkWithProducts()
        {
            var products = new List<ProductDto> { new ProductDto { Id = Guid.NewGuid(), Name = "Laptop" } };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(products);

            var controller = CreateController(_mockService);

            var result = await controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<IEnumerable<ProductDto>>>(okResult.Value);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task Create_ShouldReturnOk_WhenAdmin()
        {
            var product = new ProductDto { Id = Guid.NewGuid(), Name = "Tablet" };
            _mockService.Setup(s => s.CreateAsync(product)).ReturnsAsync(product);

            var controller = CreateController(_mockService, "Admin");

            var result = await controller.Create(product);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
            Assert.Equal("Tablet", response.Data.Name);
        }

        [Fact]
        public async Task Update_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var product = new ProductDto { Id = Guid.NewGuid(), Name = "Monitor" };
            var controller = CreateController(_mockService, "Admin");

            var result = await controller.Update(Guid.NewGuid(), product);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task AddStock_ShouldReturnBadRequest_WhenQuantityInvalid()
        {
            // Arrange
            var controller = CreateController(_mockService, "Admin");

            // Act
            var result = await controller.AddStock(Guid.NewGuid(), 0);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);

            Assert.False(response.Success);
            Assert.Equal(null, response.Message);
        }


    }
}
