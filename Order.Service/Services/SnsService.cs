using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using Order.Service.Models;
using System.Text.Json;
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
            var emailBody = BuildOrderConfirmationEmail(notification);

            var message = new
            {
                // default = "Order confirmation",
                email = new
                {
                    Subject = emailSubject,
                    Body = emailBody,
                    ToEmail = notification.UserEmail
                }
            };

            var publishRequest = new PublishRequest
            {
                TopicArn = awsConfiguration.OrderNotificationTopicArn,
                Message = JsonSerializer.Serialize(message),
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
            
            logger.LogInformation("Order confirmation email sent for order {OrderId}. MessageId: {MessageId}", 
                notification.OrderId, response.MessageId);
            
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
            var emailBody = BuildOrderStatusUpdateEmail(orderId, status);

            var message = new
            {
                // default = "Order status update",
                email = new
                {
                    Subject = emailSubject,
                    Body = emailBody,
                    ToEmail = userEmail
                }
            };

            var publishRequest = new PublishRequest
            {
                TopicArn = awsConfiguration.OrderNotificationTopicArn,
                Message = JsonSerializer.Serialize(message),
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

    private static string BuildOrderConfirmationEmail(OrderNotificationMessage notification)
    {
        var productsHtml = string.Join("", notification.Products.Select(p =>
            $"<li>{p.Name} - Quantity: {p.Quantity} - ${p.TotalPrice:F2}</li>"));

        return $@"
            <html>
            <body>
                <h2>Thank you for your order!</h2>
                <p>Dear {notification.UserName},</p>
                <p>We've received your order and it's being processed.</p>
                
                <h3>Order Details:</h3>
                <p><strong>Order ID:</strong> {notification.OrderId}</p>
                <p><strong>Order Date:</strong> {notification.OrderDate:yyyy-MM-dd HH:mm:ss}</p>
                <p><strong>Status:</strong> {notification.Status}</p>
                
                <h3>Items Ordered:</h3>
                <ul>
                    {productsHtml}
                </ul>
                
                <p><strong>Total Amount: ${notification.TotalAmount:F2}</strong></p>
                
                <p>We'll send you another email when your order ships.</p>
                <p>Thank you for shopping with us!</p>
            </body>
            </html>";
    }

    private static string BuildOrderStatusUpdateEmail(string orderId, string status)
    {
        return $@"
            <html>
            <body>
                <h2>Order Status Update</h2>
                <p>Your order #{orderId} status has been updated to: <strong>{status}</strong></p>
                
                {(status == "Shipped" ? "<p>Your order is on its way! You should receive it soon.</p>" : "")}
                {(status == "Delivered" ? "<p>Your order has been delivered! We hope you enjoy your purchase.</p>" : "")}
                {(status == "Cancelled" ? "<p>Your order has been cancelled. If you have any questions, please contact support.</p>" : "")}
                
                <p>Thank you for shopping with us!</p>
            </body>
            </html>";
    }
}