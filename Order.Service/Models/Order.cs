using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;

namespace Order.Service.Models;

[DynamoDBTable("Orders")]
public class Order
{
    [DynamoDBHashKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [DynamoDBProperty]
    [Required]
    public string UserId { get; set; } = string.Empty;

    [DynamoDBProperty]
    [Required]
    public string Products { get; set; } = string.Empty; // JSON string

    [DynamoDBProperty]
    [Required]
    public decimal Subtotal { get; set; }

    [DynamoDBProperty]
    [Required]
    public decimal DeliveryFee { get; set; }

    [DynamoDBProperty]
    public decimal TotalAmount => Subtotal + DeliveryFee;

    [DynamoDBProperty]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}