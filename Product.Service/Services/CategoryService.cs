using Product.Service.Extensions;
using Product.Service.Models.DTOs;
using Product.Service.Repositories;

namespace Product.Service.Services;

public class CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger) : ICategoryService
{
    private readonly ILogger<CategoryService> _logger = logger;

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var category = await categoryRepository.GetByIdAsync(id);
        return category?.ToResponseDto();
    }

    public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto categoryDto)
    {
        var category = categoryDto.ToEntity();
        var createdCategory = await categoryRepository.CreateAsync(category);
        return createdCategory.ToResponseDto();
    }

    public async Task<CategoryResponseDto> UpdateAsync(int id, CategoryUpdateDto categoryDto)
    {
        var existingCategory = await categoryRepository.GetByIdAsync(id);
        if (existingCategory == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        existingCategory.Name = categoryDto.Name;
        var updatedCategory = await categoryRepository.UpdateAsync(existingCategory);
        return updatedCategory.ToResponseDto();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await categoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        await categoryRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await categoryRepository.GetAllAsync();
        return categories.Select(c => c.ToResponseDto());
    }
}