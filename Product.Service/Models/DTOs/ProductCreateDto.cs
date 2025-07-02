using System.ComponentModel.DataAnnotations;

namespace Product.Service.Models.DTOs;

public class ProductCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Discounted price must be greater than 0")]
    public decimal? DiscountedPrice { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
    public int Quantity { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string? ImageUrl { get; set; }
}