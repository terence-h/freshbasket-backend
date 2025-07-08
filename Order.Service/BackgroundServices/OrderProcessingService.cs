using Order.Service.Services;

namespace Order.Service.BackgroundServices;

public class OrderProcessingService(IServiceProvider serviceProvider, ILogger<OrderProcessingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Order Processing Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var sqsService = scope.ServiceProvider.GetRequiredService<ISqsService>();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                var messages = await sqsService.ReceiveOrderMessagesAsync();

                foreach (var orderMessage in messages)
                {
                    try
                    {
                        // Process the order (update status, inventory, etc.)
                        await ProcessOrderAsync(orderMessage, orderService, sqsService);
                        
                        // Delete message
                        var deleted = await sqsService.DeleteMessageAsync(
                            Environment.GetEnvironmentVariable("SQS_QUEUE_URL")!,
                            orderMessage.ReceiptHandle
                        );

                        if (deleted)
                        {
                            logger.LogInformation("Successfully processed and deleted order message {OrderId}",
                                orderMessage.OrderId);
                        }
                        else
                        {
                            logger.LogWarning("Processed order {OrderId} but failed to delete message",
                                orderMessage.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process order {OrderId}", orderMessage.OrderId);
                    }
                }
                
                if (!messages.Any())
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in order processing service");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }


    private async Task ProcessOrderAsync(Models.OrderMessage orderMessage, IOrderService orderService,
        ISqsService sqsService)
    {
        // Update order status to "Processing"
        await orderService.UpdateOrderStatusAsync(orderMessage.OrderId, "Processing");

        // Send notification message
        var notificationMessage = new Models.OrderNotificationMessage
        {
            OrderId = orderMessage.OrderId,
            UserEmail = orderMessage.UserEmail,
            UserName = orderMessage.UserEmail, // You might want to fetch actual name
            TotalAmount = orderMessage.TotalAmount,
            OrderDate = orderMessage.CreatedAt,
            Status = "Processing",
            Products = orderMessage.Products
        };

        await sqsService.SendOrderNotificationAsync(notificationMessage);
    }
}