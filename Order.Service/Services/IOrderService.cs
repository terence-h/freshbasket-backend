using Order.Service.DTOs;

namespace Order.Service.Services;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto orderDto);
    Task<OrderResponseDto?> GetOrderByIdAsync(string id);
    Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(string userId);
    Task<OrderResponseDto?> UpdateOrderStatusAsync(string id, string status);
    Task<bool> CancelOrderAsync(string id);
    Task<List<OrderResponseDto>> GetAllOrdersAsync();
}