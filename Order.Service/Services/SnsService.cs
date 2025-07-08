using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using Order.Service.Models;
using Order.Service.Models.Configurations;

namespace Order.Service.Services;

public class SnsService(
    IAmazonSimpleNotificationService snsClient,
    IOptions<AwsConfiguration> awsConfiguration,
    ILogger<SnsService> logger)
    : ISnsService
{
    private readonly AwsConfiguration awsConfiguration = awsConfiguration.Value;

    public async Task<bool> SendOrderConfirmationEmailAsync(OrderNotificationMessage notification)
    {
        try
        {
            var emailSubject = $"Order Confirmation - Order #{notification.OrderId}";
            var emailBody = BuildOrderConfirmationEmailPlainText(notification);

            var publishRequest = new PublishRequest
            {
                TopicArn = awsConfiguration.OrderNotificationTopicArn,
                Subject = emailSubject,
                Message = emailBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "email",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = notification.UserEmail
                        }
                    },
                    {
                        "orderId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = notification.OrderId
                        }
                    }
                }
            };

            var response = await snsClient.PublishAsync(publishRequest);
            return !string.IsNullOrEmpty(response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", notification.OrderId);
            return false;
        }
    }

    public async Task<bool> SendOrderStatusUpdateEmailAsync(string orderId, string userEmail, string status)
    {
        try
        {
            var emailSubject = $"Order Status Update - Order #{orderId}";
            var emailBody = BuildOrderStatusUpdateEmailPlainText(orderId, status); // ✅ Changed to plain text

            var publishRequest = new PublishRequest
            {
                TopicArn = awsConfiguration.OrderNotificationTopicArn,
                Subject = emailSubject,
                Message = emailBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "email",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = userEmail
                        }
                    },
                    {
                        "orderId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = orderId
                        }
                    }
                }
            };

            var response = await snsClient.PublishAsync(publishRequest);

            logger.LogInformation("Order status update email sent for order {OrderId}. MessageId: {MessageId}",
                orderId, response.MessageId);

            return !string.IsNullOrEmpty(response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order status update email for order {OrderId}", orderId);
            return false;
        }
    }

    private static string BuildOrderConfirmationEmailPlainText(OrderNotificationMessage notification)
    {
        var productsText = string.Join("\n", notification.Products.Select(p =>
            $"• {p.Name} - Quantity: {p.Quantity} - ${p.TotalPrice:F2}"));

        return $@"Thank you for your order!

Dear Customer,

We've received your order and it's being processed.

Order Details:
Order ID: {notification.OrderId}
Order Date: {notification.OrderDate:yyyy-MM-dd HH:mm:ss}
Status: {notification.Status}

Items Ordered:
{productsText}

Total Amount: ${notification.TotalAmount:F2}

We'll send you another email when your order ships.
Thank you for shopping with Fresh Basket!";
    }

    private static string BuildOrderStatusUpdateEmailPlainText(string orderId, string status)
    {
        var statusMessage = status switch
        {
            "Shipped" => "Your order is on its way! You should receive it soon.",
            "Delivered" => "Your order has been delivered! We hope you enjoy your purchase.",
            "Cancelled" => "Your order has been cancelled. If you have any questions, please contact support.",
            _ => "Your order status has been updated."
        };

        return $@"Order Status Update

Your order #{orderId} status has been updated to: {status.ToUpper()}

{statusMessage}

Thank you for shopping with Fresh Basket!

If you have any questions, please contact our support team.";
    }
}