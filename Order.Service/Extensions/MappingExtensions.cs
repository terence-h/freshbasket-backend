using System.Text.Json;
using Order.Service.DTOs;

namespace Order.Service.Extensions;

public static class MappingExtensions
{
    public static OrderResponseDto ToResponseDto(this Models.Order order)
    {
        var products = string.IsNullOrEmpty(order.Products)
            ? new List<OrderProductDto>()
            : JsonSerializer.Deserialize<List<OrderProductDto>>(order.Products) ?? new List<OrderProductDto>();

        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Products = products,
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}