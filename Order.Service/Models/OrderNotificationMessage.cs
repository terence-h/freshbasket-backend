using System.Text.Json.Serialization;

namespace Order.Service.Models;

public class OrderNotificationMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderProductMessage> Products { get; set; } = new();
    
    [JsonIgnore]
    public string ReceiptHandle { get; set; } = string.Empty;
}