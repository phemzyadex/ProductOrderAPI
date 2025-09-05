using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductOrderAPI.Api.Controllers;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
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
            // Arrange
            var products = new List<ProductRequestDto>
    {
        new ProductRequestDto { Name = "Laptop" }
    };

            var expectedResponse = new ApiResponse<IEnumerable<ProductRequestDto>>(
                true,
                products,
                "Products retrieved successfully"
            );

            _mockService
                .Setup(s => s.GetAllAsync());
                //.ReturnsAsync(expectedResponse);

            var controller = CreateController(_mockService);

            // Act
            var result = await controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            //var response = Assert.IsType<ApiResponse<IEnumerable<ProductDto>>>(okResult.Value);
            //var createdProduct = Assert.Single(response.Data);
            //Assert.Equal("Laptop", createdProduct.Name);
        }


        [Fact]
        public async Task Create_ShouldReturnOk_WhenAdmin()
        {
            // Arrange
            var product = new ProductRequestDto { Name = "Tablet" };

            var expectedResponse = new ApiResponse<ProductRequestDto>(
                true,
                product,
                "Product created"
            );

            _mockService
                .Setup(s => s.CreateAsync(It.IsAny<ProductRequestDto>()));
                //.ReturnsAsync(expectedResponse);

            var controller = CreateController(_mockService, "Admin");

            // Act
            var result = await controller.Create(product);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            //var response = Assert.IsType<ApiResponse<ProductDto>>(okResult.Value);
            //Assert.Equal("Tablet", response.Data.Name);


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
            Assert.Equal("Quantity must be greater than zero", response.Message);
        }


    }
}
