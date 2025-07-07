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
    public async Task<ProductResponseDto?> GetByIdAsync(string id)
    {
        var product = await productRepository.GetByIdAsync(id);
        if (product == null) return null;

        var productDto = product.ToResponseDto();
        
        // Get category name
        var category = await categoryRepository.GetByIdAsync(product.CategoryId);
        productDto.CategoryName = category?.Name;

        // Get image download URL if image exists
        if (!string.IsNullOrEmpty(product.ImageKey))
        {
            productDto.ImageUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageKey);
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
        
        // Store the key if image is uploaded
        if (productDto.Image != null)
        {
            var uploadResult = await s3Service.UploadImageAsync(productDto.Image);
            product.ImageKey = uploadResult.Key;
        }
    
        var createdProduct = await productRepository.CreateAsync(product);
    
        var responseDto = createdProduct.ToResponseDto();
        responseDto.CategoryName = category.Name;

        // Generate download URL from stored key
        if (!string.IsNullOrEmpty(createdProduct.ImageKey))
        {
            responseDto.ImageUrl = await s3Service.GetPreSignedDownloadUrlAsync(createdProduct.ImageKey);
        }

        return responseDto;
    }


    public async Task<ProductResponseDto> UpdateAsync(string id, ProductUpdateDto productDto)
    {
        var existingProduct = await productRepository.GetByIdAsync(id);
        if (existingProduct == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        // Update product properties
        existingProduct.Name = productDto.Name;
        existingProduct.Description = productDto.Description;
        existingProduct.Price = productDto.Price;
        existingProduct.DiscountedPrice = productDto.DiscountedPrice;
        existingProduct.Quantity = productDto.Quantity;
        existingProduct.CategoryId = productDto.CategoryId;
        
        if (productDto.Image != null)
        {
            var uploadImage = await s3Service.UploadImageAsync(productDto.Image);
            existingProduct.ImageKey = uploadImage.Key;
        }

        var updatedProduct = await productRepository.UpdateAsync(existingProduct);
        
        var responseDto = updatedProduct.ToResponseDto();
        
        // Get category name
        var category = await categoryRepository.GetByIdAsync(existingProduct.CategoryId);
        responseDto.CategoryName = category?.Name;

        // Get image download URL if image exists
        if (!string.IsNullOrEmpty(updatedProduct.ImageKey))
        {
            responseDto.ImageUrl = await s3Service.GetPreSignedDownloadUrlAsync(updatedProduct.ImageKey);
        }

        return responseDto;
    }

    public async Task DeleteAsync(string id)
    {
        var product = await productRepository.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id}");

        // Delete associated image if exists
        if (!string.IsNullOrEmpty(product.ImageKey))
        {
            try
            {
                await s3Service.DeleteImageAsync(product.ImageKey);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete image {ImageUrl} for product {ProductId}", product.ImageKey, id);
            }
        }

        await productRepository.DeleteAsync(id);
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
            if (!string.IsNullOrEmpty(product.ImageKey))
            {
                try
                {
                    productDto.ImageUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate download URL for image {ImageKey}", product.ImageKey);
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
            if (!string.IsNullOrEmpty(product.ImageKey))
            {
                try
                {
                    productDto.ImageUrl = await s3Service.GetPreSignedDownloadUrlAsync(product.ImageKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to generate download URL for image {ImageKey}", product.ImageKey);
                }
            }
            
            result.Add(productDto);
        }

        return result;
    }
}
