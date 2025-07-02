using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;

namespace Product.Service.Models;

[DynamoDBTable("Products")]
public class Product
{
    [DynamoDBHashKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [DynamoDBRangeKey]
    public int CategoryId { get; set; }

    [DynamoDBProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty]
    [Required]
    public string Description { get; set; } = string.Empty;

    [DynamoDBProperty]
    [Required]
    public decimal Price { get; set; }

    [DynamoDBProperty]
    public decimal? DiscountedPrice { get; set; }

    [DynamoDBProperty]
    [Required]
    public int Quantity { get; set; }

    [DynamoDBProperty]
    public string? ImageUrl { get; set; }

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}