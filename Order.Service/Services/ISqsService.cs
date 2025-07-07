using Order.Service.Models;

namespace Order.Service.Services;

public interface ISqsService
{
    Task<bool> SendOrderForProcessingAsync(OrderMessage orderMessage);
    Task<bool> SendOrderNotificationAsync(OrderNotificationMessage notificationMessage);
    Task<List<OrderMessage>> ReceiveOrderMessagesAsync();
    Task<List<OrderNotificationMessage>> ReceiveNotificationMessagesAsync();
    Task<bool> DeleteMessageAsync(string queueUrl, string receiptHandle);
}