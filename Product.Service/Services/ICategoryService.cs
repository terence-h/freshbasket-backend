using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public interface ICategoryService
{
    Task<CategoryResponseDto?> GetByIdAsync(int id);
    Task<CategoryResponseDto> CreateAsync(CategoryCreateDto categoryDto);
    Task<CategoryResponseDto> UpdateAsync(int id, CategoryUpdateDto categoryDto);
    Task DeleteAsync(int id);
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
}