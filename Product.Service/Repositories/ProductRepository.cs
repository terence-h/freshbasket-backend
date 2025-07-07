namespace Product.Service.Repositories;

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Models;

public class ProductRepository(IDynamoDBContext dynamoDbContext, ILogger<ProductRepository> logger) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(string id)
    {
        try
        {
            // Query all items with this ID (across all categories)
            var search = dynamoDbContext.QueryAsync<Product>(id);
            var results = await search.GetNextSetAsync();
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product by ID {ProductId}", id);
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

    public async Task DeleteAsync(string id)
    {
        try
        {
            await dynamoDbContext.DeleteAsync<Product>(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product {ProductId}", id);
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
            var search = dynamoDbContext.QueryAsync<Product>(
                categoryId,
                new DynamoDBOperationConfig 
                { 
                    IndexName = "CategoryId-index" 
                });
            return await search.GetRemainingAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products by CategoryId {CategoryId}", categoryId);
            throw;
        }
    }
}
