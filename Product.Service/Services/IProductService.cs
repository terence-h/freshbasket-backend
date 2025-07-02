using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public interface IProductService
{
    Task<ProductResponseDto?> GetByIdAsync(string id, int categoryId);
    Task<ProductResponseDto> CreateAsync(ProductCreateDto productDto);
    Task<ProductResponseDto> UpdateAsync(string id, int categoryId, ProductUpdateDto productDto);
    Task DeleteAsync(string id, int categoryId);
    Task<IEnumerable<ProductResponseDto>> GetAllAsync();
    Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(int categoryId);
}