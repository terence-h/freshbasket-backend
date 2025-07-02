using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;

namespace Product.Service.Models;

[DynamoDBTable("Categories")]
public class Category
{
    [DynamoDBHashKey]
    public int Id { get; set; }

    [DynamoDBProperty]
    [Required]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}