namespace Product.Service.Repositories;

using Models;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(string id, int categoryId);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(string id, int categoryId);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> GetByCategoryIdAsync(int categoryId);
}