using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace Order.Service.Repositories;

public class OrderRepository(IDynamoDBContext context) : IOrderRepository
{
    public async Task<Models.Order> CreateAsync(Models.Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveAsync(order);
        return order;
    }

    public async Task<Models.Order?> GetByIdAsync(string id)
    {
        return await context.LoadAsync<Models.Order>(id);
    }

    public async Task<List<Models.Order>> GetByUserIdAsync(string userId)
    {
        var scanConditions = new List<ScanCondition>
        {
            new ScanCondition("UserId", ScanOperator.Equal, userId)
        };

        var search = context.ScanAsync<Models.Order>(scanConditions);
        return await search.GetRemainingAsync();
    }

    public async Task<Models.Order> UpdateAsync(Models.Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveAsync(order);
        return order;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var order = await GetByIdAsync(id);
        if (order == null) return false;

        await context.DeleteAsync(order);
        return true;
    }

    public async Task<List<Models.Order>> GetAllAsync()
    {
        var search = context.ScanAsync<Models.Order>(new List<ScanCondition>());
        return await search.GetRemainingAsync();
    }
}