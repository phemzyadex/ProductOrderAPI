using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;

namespace ProductOrderAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service) => _service = service;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<ProductDto>>.Ok(products));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<ProductDto>.Ok(product));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ProductRequestDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return Ok(ApiResponse<ProductDto>.Ok(created, "Product created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductDto dto)
        {
            if (id != dto.Id)
                return BadRequest(ApiResponse<string>.Fail(null, "Mismatched product ID"));

            var updated = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProductDto>.Ok(updated, "Product updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok(null, "Product deleted successfully"));
        }

        [HttpPost("{id:guid}/add-stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStock(Guid id, [FromQuery] int quantity)
        {
            if (quantity <= 0)
                return BadRequest(ApiResponse<string>.Fail(null, "Quantity must be greater than zero"));

            var updated = await _service.AddStockAsync(id, quantity);
            return Ok(ApiResponse<ProductDto>.Ok(updated, "Stock added successfully"));
        }
    }
}
