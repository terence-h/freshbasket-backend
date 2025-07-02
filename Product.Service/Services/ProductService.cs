using Product.Service.Extensions;
using Product.Service.Models.DTOs;
using Product.Service.Repositories;

namespace Product.Service.Services;

public class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IS3Service s3Service,
    ILogger<ProductService> logger)
    : IProductService
{
    public async Task<ProductResponseDto?> GetByIdAsync(string id, int categoryId)
    {
        var product = await productRepository.GetByIdAsync(id, categoryId);
        if (product == null) return null;

        var productDto = product.ToResponseDto();
        
        // Get category name
        var category = await categoryRepository.GetByIdAsync(categoryId);
        productDto.CategoryName = category?.Name;

        // Get image download URL if image exists
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            productDto.ImageDownloadUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageUrl);
        }

        return productDto;
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto productDto)
    {
        // Validate category exists
        var category = await categoryRepository.GetByIdAsync(productDto.CategoryId);
        if (category == null)
            throw new ArgumentException($"Category with ID {productDto.CategoryId} not found");

        var product = productDto.ToEntity();
        var createdProduct = await productRepository.CreateAsync(product);
        
        var responseDto = createdProduct.ToResponseDto();
        responseDto.CategoryName = category.Name;

        // Get image download URL if image exists
        if (!string.IsNullOrEmpty(createdProduct.ImageUrl))
        {
            responseDto.ImageDownloadUrl = await s3Service.GetPreSignedDownloadUrlAsync(createdProduct.ImageUrl);
        }

        return responseDto;
    }

    public async Task<ProductResponseDto> UpdateAsync(string id, int categoryId, ProductUpdateDto productDto)
    {
        var existingProduct = await productRepository.GetByIdAsync(id, categoryId);
        if (existingProduct == null)
            throw new KeyNotFoundException($"Product with ID {id} and CategoryId {categoryId} not found");

        // Update product properties
        existingProduct.Name = productDto.Name;
        existingProduct.Description = productDto.Description;
        existingProduct.Price = productDto.Price;
        existingProduct.DiscountedPrice = productDto.DiscountedPrice;
        existingProduct.Quantity = productDto.Quantity;
        existingProduct.ImageUrl = productDto.ImageUrl;

        var updatedProduct = await productRepository.UpdateAsync(existingProduct);
        
        var responseDto = updatedProduct.ToResponseDto();
        
        // Get category name
        var category = await categoryRepository.GetByIdAsync(categoryId);
        responseDto.CategoryName = category?.Name;

        // Get image download URL if image exists
        if (!string.IsNullOrEmpty(updatedProduct.ImageUrl))
        {
            responseDto.ImageDownloadUrl = await s3Service.GetPreSignedDownloadUrlAsync(updatedProduct.ImageUrl);
        }

        return responseDto;
    }

    public async Task DeleteAsync(string id, int categoryId)
    {
        var product = await productRepository.GetByIdAsync(id, categoryId);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} and CategoryId {categoryId} not found");

        // Delete associated image if exists
        if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            try
            {
                await s3Service.DeleteImageAsync(product.ImageUrl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete image {ImageUrl} for product {ProductId}", product.ImageUrl, id);
            }
        }

        await productRepository.DeleteAsync(id, categoryId);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
    {
        var products = await productRepository.GetAllAsync();
        var categories = await categoryRepository.GetAllAsync();
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        var result = new List<ProductResponseDto>();
        
        foreach (var product in products)
        {
            var productDto = product.ToResponseDto();
            productDto.CategoryName = categoryDict.GetValueOrDefault(product.CategoryId);
            
            // Get image download URL if image exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                try
                {
                    productDto.ImageDownloadUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageUrl);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate download URL for image {ImageUrl}", product.ImageUrl);
                }
            }
            
            result.Add(productDto);
        }

        return result;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(int categoryId)
    {
        var products = await productRepository.GetByCategoryIdAsync(categoryId);
        var category = await categoryRepository.GetByIdAsync(categoryId);

        var result = new List<ProductResponseDto>();
        
        foreach (var product in products)
        {
            var productDto = product.ToResponseDto();
            productDto.CategoryName = category?.Name;
            
            // Get image download URL if image exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                try
                {
                    productDto.ImageDownloadUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageUrl);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate download URL for image {ImageUrl}", product.ImageUrl);
                }
            }
            
            result.Add(productDto);
        }

        return result;
    }
}
