using Order.Service.Models;

namespace Order.Service.Services;

public interface ISnsService
{
    Task<bool> SendOrderConfirmationEmailAsync(OrderNotificationMessage notification);
    Task<bool> SendOrderStatusUpdateEmailAsync(string orderId, string userEmail, string status);
}