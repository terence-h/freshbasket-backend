using System.Text.Json.Serialization;

namespace Order.Service.Models;

public class OrderMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderProductMessage> Products { get; set; } = new();
    
    [JsonIgnore]
    public string ReceiptHandle { get; set; } = string.Empty;
}

public class OrderProductMessage
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}