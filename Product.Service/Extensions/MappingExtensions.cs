namespace Product.Service.Extensions;

using Models.DTOs;
using Models;

public static class MappingExtensions
{
    public static ProductResponseDto ToResponseDto(this Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountedPrice = product.DiscountedPrice,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public static Product ToEntity(this ProductCreateDto dto)
    {
        return new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DiscountedPrice = dto.DiscountedPrice,
            Quantity = dto.Quantity,
            CategoryId = dto.CategoryId
        };
    }

    public static CategoryResponseDto ToResponseDto(this Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public static Category ToEntity(this CategoryCreateDto dto)
    {
        return new Category
        {
            Name = dto.Name
        };
    }
}