namespace Product.Service.Repositories;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Models;

public class ProductRepository(IDynamoDBContext dynamoDbContext, ILogger<ProductRepository> logger) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(string id, int categoryId)
    {
        try
        {
            return await dynamoDbContext.LoadAsync<Product>(id, categoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product by ID {ProductId}, CategoryId {CategoryId}", id, categoryId);
            throw;
        }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        try
        {
            await dynamoDbContext.SaveAsync(product);
            return product;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product {ProductId}", product.Id);
            throw;
        }
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        try
        {
            product.UpdatedAt = DateTime.UtcNow;
            await dynamoDbContext.SaveAsync(product);
            return product;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}", product.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string id, int categoryId)
    {
        try
        {
            await dynamoDbContext.DeleteAsync<Product>(id, categoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product {ProductId}, CategoryId {CategoryId}", id, categoryId);
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        try
        {
            var search = dynamoDbContext.ScanAsync<Product>(new List<ScanCondition>());
            return await search.GetRemainingAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all products");
            throw;
        }
    }

    public async Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId)
    {
        try
        {
            var scanConditions = new List<ScanCondition>
            {
                new("CategoryId", ScanOperator.Equal, categoryId)
            };

            var search = dynamoDbContext.ScanAsync<Product>(scanConditions);
            return await search.GetRemainingAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            throw;
        }
    }
}
