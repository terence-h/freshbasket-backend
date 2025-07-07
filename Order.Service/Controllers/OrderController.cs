using Microsoft.AspNetCore.Mvc;
using Order.Service.DTOs;
using Order.Service.Services;

namespace Order.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderCreateDto orderDto)
    {
        try
        {
            var order = await orderService.CreateOrderAsync(orderDto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the order", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(string id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }

        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrdersByUser(string userId)
    {
        var orders = await orderService.GetOrdersByUserIdAsync(userId);
        return Ok(orders);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        var orders = await orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var order = await orderService.UpdateOrderStatusAsync(id, dto.Status);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelOrder(string id)
    {
        try
        {
            var success = await orderService.CancelOrderAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(new { message = "Order cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
}
