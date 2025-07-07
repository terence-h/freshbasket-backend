namespace Order.Service.DTOs;

public class OrderResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<OrderProductDto> Products { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}