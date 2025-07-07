using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public interface IProductService
{
    Task<ProductResponseDto?> GetByIdAsync(string id);
    Task<ProductResponseDto> CreateAsync(ProductCreateDto productDto);
    Task<ProductResponseDto> UpdateAsync(string id, ProductUpdateDto productDto);
    Task DeleteAsync(string id);
    Task<IEnumerable<ProductResponseDto>> GetAllAsync();
    Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(int categoryId);
}