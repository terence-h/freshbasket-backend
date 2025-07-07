using System.ComponentModel.DataAnnotations;

namespace Order.Service.DTOs;

public class OrderCreateDto
{
    [Required]
    public string AuthToken { get; set; } = string.Empty; // JWT token for user validation

    [Required]
    [MinLength(1, ErrorMessage = "At least one product is required")]
    public List<OrderProductDto> Products { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "Delivery fee must be non-negative")]
    public decimal DeliveryFee { get; set; } = 5.00m; // Default delivery fee
}