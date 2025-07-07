namespace Order.Service.Models.Configurations;

public class AwsConfiguration
{
    public string OrderProcessingQueueUrl { get; set; } = string.Empty;
    public string OrderNotificationQueueUrl { get; set; } = string.Empty;
    public string OrderNotificationTopicArn { get; set; } = string.Empty;
    public string UserServiceBaseUrl { get; set; } = string.Empty;
}