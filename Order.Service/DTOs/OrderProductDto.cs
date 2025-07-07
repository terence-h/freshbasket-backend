using System.ComponentModel.DataAnnotations;

namespace Order.Service.DTOs;

public class OrderProductDto
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    public decimal TotalPrice => Price * Quantity;
}