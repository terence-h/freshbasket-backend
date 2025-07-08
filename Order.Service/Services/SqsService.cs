using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Order.Service.Models;
using System.Text.Json;
using Order.Service.Models.Configurations;

namespace Order.Service.Services;

public class SqsService(IAmazonSQS sqsClient, IOptions<AwsConfiguration> awsConfiguration, ILogger<SqsService> logger)
    : ISqsService
{
    private readonly AwsConfiguration awsConfiguration = awsConfiguration.Value;

    public async Task<bool> SendOrderForProcessingAsync(OrderMessage orderMessage)
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(orderMessage);

            var request = new SendMessageRequest
            {
                QueueUrl = awsConfiguration.OrderProcessingQueueUrl,
                MessageBody = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "OrderId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = orderMessage.OrderId
                        }
                    },
                    {
                        "UserId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = orderMessage.UserId
                        }
                    }
                }
            };

            var response = await sqsClient.SendMessageAsync(request);

            logger.LogInformation("Order {OrderId} sent to processing queue. MessageId: {MessageId}",
                orderMessage.OrderId, response.MessageId);

            return !string.IsNullOrEmpty(response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order {OrderId} to processing queue", orderMessage.OrderId);
            return false;
        }
    }

    public async Task<bool> SendOrderNotificationAsync(OrderNotificationMessage notificationMessage)
    {
        try
        {
            var messageBody = JsonSerializer.Serialize(notificationMessage);

            var request = new SendMessageRequest
            {
                QueueUrl = awsConfiguration.OrderNotificationQueueUrl,
                MessageBody = messageBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                        "OrderId",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = notificationMessage.OrderId
                        }
                    },
                    {
                        "UserEmail",
                        new MessageAttributeValue
                        {
                            DataType = "String",
                            StringValue = notificationMessage.UserEmail
                        }
                    }
                }
            };

            var response = await sqsClient.SendMessageAsync(request);

            logger.LogInformation("Notification for order {OrderId} sent to notification queue. MessageId: {MessageId}",
                notificationMessage.OrderId, response.MessageId);

            return !string.IsNullOrEmpty(response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification for order {OrderId} to queue",
                notificationMessage.OrderId);
            return false;
        }
    }

    public async Task<List<OrderMessage>> ReceiveOrderMessagesAsync()
    {
        try
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = awsConfiguration.OrderProcessingQueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20,
                MessageAttributeNames = new List<string> { "All" }
            };

            var response = await sqsClient.ReceiveMessageAsync(request);
            var messages = new List<OrderMessage>();

            foreach (var message in response.Messages)
            {
                try
                {
                    var orderMessage = JsonSerializer.Deserialize<OrderMessage>(message.Body);
                    if (orderMessage != null)
                    {
                        // Add ReceiptHandle to the message for deletion later
                        orderMessage.ReceiptHandle = message.ReceiptHandle;
                        messages.Add(orderMessage);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to deserialize order message: {MessageBody}", message.Body);
                    // Delete malformed messages
                    await DeleteMessageAsync(awsConfiguration.OrderProcessingQueueUrl, message.ReceiptHandle);
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive order messages from queue");
            return new List<OrderMessage>();
        }
    }

    public async Task<List<OrderNotificationMessage>> ReceiveNotificationMessagesAsync()
    {
        try
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = awsConfiguration.OrderNotificationQueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20,
                MessageAttributeNames = new List<string> { "All" }
            };

            var response = await sqsClient.ReceiveMessageAsync(request);
            var messages = new List<OrderNotificationMessage>();

            foreach (var message in response.Messages)
            {
                try
                {
                    var notificationMessage = JsonSerializer.Deserialize<OrderNotificationMessage>(message.Body);
                    if (notificationMessage != null)
                    {
                        // Add ReceiptHandle to the message for deletion later
                        notificationMessage.ReceiptHandle = message.ReceiptHandle;
                        messages.Add(notificationMessage);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Failed to deserialize notification message: {MessageBody}", message.Body);
                    // Delete malformed messages
                    await DeleteMessageAsync(awsConfiguration.OrderNotificationQueueUrl, message.ReceiptHandle);
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive notification messages from queue");
            return new List<OrderNotificationMessage>();
        }
    }


    public async Task<bool> DeleteMessageAsync(string queueUrl, string receiptHandle)
    {
        try
        {
            await sqsClient.DeleteMessageAsync(queueUrl, receiptHandle);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete message with receipt handle: {ReceiptHandle}", receiptHandle);
            return false;
        }
    }
}