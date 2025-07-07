namespace Order.Service.Repositories;

using Order.Service.Models;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Models.Order order);
    Task<Order?> GetByIdAsync(string id);
    Task<List<Order>> GetByUserIdAsync(string userId);
    Task<Order> UpdateAsync(Models.Order order);
    Task<bool> DeleteAsync(string id);
    Task<List<Order>> GetAllAsync();
}