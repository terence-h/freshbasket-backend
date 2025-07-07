using System.Text.Json;
using Order.Service.DTOs;
using Order.Service.Extensions;
using Order.Service.Models;
using Order.Service.Repositories;

namespace Order.Service.Services;

public class OrderService(
    IOrderRepository orderRepository,
    ISqsService sqsService,
    ISnsService snsService,
    IUserService userService,
    ILogger<OrderService> logger)
    : IOrderService
{
    public async Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto orderDto)
    {
        // Validate token and get user details
        var tokenValidation = await userService.ValidateTokenAsync(orderDto.AuthToken);
        if (tokenValidation == null || !tokenValidation.IsValid)
        {
            throw new UnauthorizedAccessException("Invalid or expired token");
        }

        // Calculate subtotal
        var subtotal = orderDto.Products.Sum(p => p.TotalPrice);

        // Create order entity
        var order = new Models.Order
        {
            UserId = tokenValidation.UserId,
            Products = JsonSerializer.Serialize(orderDto.Products),
            Subtotal = subtotal,
            DeliveryFee = orderDto.DeliveryFee,
            Status = "Pending"
        };

        // Mock payment processing
        await ProcessPaymentAsync(order.TotalAmount);

        // Save order
        var createdOrder = await orderRepository.CreateAsync(order);

        // Send order to SQS for processing
        var orderMessage = new OrderMessage
        {
            OrderId = createdOrder.Id,
            UserId = createdOrder.UserId,
            UserEmail = tokenValidation.Email,
            TotalAmount = createdOrder.TotalAmount,
            CreatedAt = createdOrder.CreatedAt,
            Products = orderDto.Products.Select(p => new OrderProductMessage
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                TotalPrice = p.TotalPrice
            }).ToList()
        };

        var success = await sqsService.SendOrderForProcessingAsync(orderMessage);

        if (success)
        {
            logger.LogInformation("Order {OrderId} sent for processing", createdOrder.Id);
        }
        else
        {
            logger.LogWarning("Failed to send order {OrderId} for processing", createdOrder.Id);
        }

        // Return response
        return createdOrder.ToResponseDto();
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(string id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        return order?.ToResponseDto();
    }

    public async Task<List<OrderResponseDto>> GetOrdersByUserIdAsync(string userId)
    {
        var orders = await orderRepository.GetByUserIdAsync(userId);
        return orders.Select(o => o.ToResponseDto()).ToList();
    }

    public async Task<OrderResponseDto?> UpdateOrderStatusAsync(string id, string status)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return null;

        // Validate status
        var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");
        }

        var oldStatus = order.Status;
        order.Status = status;
        var updatedOrder = await orderRepository.UpdateAsync(order);

        // Send status update notification if status changed
        if (oldStatus != status && status != "Processing") // Processing notifications are handled by background service
        {
            try
            {
                var userEmail = await userService.GetUserEmailAsync(order.UserId);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await snsService.SendOrderStatusUpdateEmailAsync(order.Id, userEmail, status);
                    logger.LogInformation("Status update notification sent for order {OrderId}", order.Id);
                }
                else
                {
                    logger.LogWarning("Could not retrieve user email for order {OrderId} status update", order.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send status update notification for order {OrderId}", order.Id);
            }
        }

        return updatedOrder.ToResponseDto();
    }

    public async Task<bool> CancelOrderAsync(string id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return false;

        // Only allow cancellation if order is still pending or processing
        if (order.Status is "Shipped" or "Delivered" or "Cancelled")
        {
            throw new InvalidOperationException($"Cannot cancel order with status: {order.Status}");
        }

        order.Status = "Cancelled";
        await orderRepository.UpdateAsync(order);

        // Send cancellation notification
        try
        {
            var userEmail = await userService.GetUserEmailAsync(order.UserId);
            if (!string.IsNullOrEmpty(userEmail))
            {
                await snsService.SendOrderStatusUpdateEmailAsync(order.Id, userEmail, "Cancelled");
                logger.LogInformation("Cancellation notification sent for order {OrderId}", order.Id);
            }
            else
            {
                logger.LogWarning("Could not retrieve user email for order {OrderId} cancellation notification",
                    order.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send cancellation notification for order {OrderId}", order.Id);
        }

        return true;
    }

    public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await orderRepository.GetAllAsync();
        return orders.Select(o => o.ToResponseDto()).ToList();
    }

    // Mock payment processing
    private async Task ProcessPaymentAsync(decimal amount)
    {
        // Simulate payment processing delay
        await Task.Delay(500);

        // Mock payment success (you could add random failures for testing)
        var success = true; // Random.Shared.NextDouble() > 0.1; // 90% success rate

        if (!success)
        {
            throw new InvalidOperationException("Payment processing failed");
        }
    }
}