using System.ComponentModel.DataAnnotations;

namespace Product.Service.Models.DTOs;

public class CategoryUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}