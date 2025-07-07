using Order.Service.Services;

namespace Order.Service.BackgroundServices;

public class NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var sqsService = scope.ServiceProvider.GetRequiredService<ISqsService>();
                var snsService = scope.ServiceProvider.GetRequiredService<ISnsService>();

                var messages = await sqsService.ReceiveNotificationMessagesAsync();

                foreach (var notification in messages)
                {
                    try
                    {
                        // Send email notification via SNS
                        var success = await snsService.SendOrderConfirmationEmailAsync(notification);

                        if (success)
                        {
                            logger.LogInformation("Successfully sent notification for order {OrderId}", notification.OrderId);
                        }
                        else
                        {
                            logger.LogWarning("Failed to send notification for order {OrderId}", notification.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process notification for order {OrderId}", notification.OrderId);
                    }
                }

                // If no messages, wait before polling again
                if (!messages.Any())
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in notification service");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
