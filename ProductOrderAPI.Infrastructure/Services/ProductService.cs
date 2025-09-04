using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductOrderAPI.Application.DTOs;
using ProductOrderAPI.Application.Interfaces;
using ProductOrderAPI.Domain.Entities;
using ProductOrderAPI.Infrastructure.Persistence;
using System.Security.Claims;

namespace ProductOrderAPI.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ProductService> _logger;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(
            AppDbContext db,
            ILogger<ProductService> logger,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _logger = logger;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUsername() =>
            _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "system";
        public async Task<ProductDto> CreateAsync(ProductRequestDto dto)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CreatedDate = DateTime.UtcNow,
                RowVersion = Array.Empty<byte>() // handled by DB
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            await _auditService.LogEventAsync("ProductCreated", GetCurrentUsername(),
                $"Product created. Id: {product.Id}, Name: {product.Name}");

            return ToDto(product);
        }

        public async Task<ProductDto> UpdateAsync(Guid id, ProductDto dto)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            // Concurrency check
            if (!dto.RowVersion.SequenceEqual(product.RowVersion))
                throw new DbUpdateConcurrencyException("The product has been modified by another user.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;

            _db.Entry(product).OriginalValues["RowVersion"] = dto.RowVersion;

            await _db.SaveChangesAsync();

            await _auditService.LogEventAsync("ProductUpdated", GetCurrentUsername(),
                $"Product updated. Id: {product.Id}, Name: {product.Name}");

            return ToDto(product);
        }
        public async Task DeleteAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            await _auditService.LogEventAsync("ProductDeleted", GetCurrentUsername(),
                $"Product deleted. Id: {id}");
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _db.Products.ToListAsync();
            return products.Select(ToDto);
        }

        public async Task<ProductDto> GetByIdAsync(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) throw new KeyNotFoundException("Product not found.");
            return ToDto(product);
        }

        public async Task<ProductDto> AddStockAsync(Guid productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            // prevent negative or zero
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            product.StockQuantity += quantity;

            await _db.SaveChangesAsync();

            await _auditService.LogEventAsync("StockUpdated", GetCurrentUsername(),
                $"Stock updated. ProductId: {product.Id}, Added: {quantity}, NewStock: {product.StockQuantity}");

            return ToDto(product);
        }

        private static ProductDto ToDto(Product product) =>
            new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                RowVersion = product.RowVersion
            };
    }
}
