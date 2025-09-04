using ProductOrderAPI.Application.DTOs;

namespace ProductOrderAPI.Application.Interfaces
{
    public interface IProductService
    {
        Task<ProductDto> CreateAsync(ProductRequestDto dto);
        Task<ProductDto> UpdateAsync(Guid id, ProductDto dto);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto> GetByIdAsync(Guid id);
        Task<ProductDto> AddStockAsync(Guid productId, int quantity);
    }
}
