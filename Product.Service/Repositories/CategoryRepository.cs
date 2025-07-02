using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Product.Service.Models;

namespace Product.Service.Repositories;

public class CategoryRepository(IDynamoDBContext dynamoDbContext, ILogger<CategoryRepository> logger) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(int id)
    {
        try
        {
            return await dynamoDbContext.LoadAsync<Category>(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting category by ID {CategoryId}", id);
            throw;
        }
    }

    public async Task<Category> CreateAsync(Category category)
    {
        try
        {
            if (category.Id == 0)
            {
                category.Id = await GetNextIdAsync();
            }
            
            await dynamoDbContext.SaveAsync(category);
            return category;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        try
        {
            category.UpdatedAt = DateTime.UtcNow;
            await dynamoDbContext.SaveAsync(category);
            return category;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", category.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            await dynamoDbContext.DeleteAsync<Category>(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category {CategoryId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        try
        {
            var search = dynamoDbContext.ScanAsync<Category>(new List<ScanCondition>());
            return await search.GetRemainingAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all categories");
            throw;
        }
    }

    public async Task<int> GetNextIdAsync()
    {
        try
        {
            var allCategories = await GetAllAsync();
            return allCategories.Any() ? allCategories.Max(c => c.Id) + 1 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting next category ID");
            throw;
        }
    }
}
