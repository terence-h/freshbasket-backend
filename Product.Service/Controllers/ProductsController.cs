using Microsoft.AspNetCore.Mvc;
using Product.Service.Models.DTOs;
using Product.Service.Services;

namespace Product.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    IProductService productService,
    IAuthService authService,
    ILogger<ProductsController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll()
    {
        try
        {
            var products = await productService.GetAllAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all products");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}/{categoryId}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(string id, int categoryId)
    {
        try
        {
            var product = await productService.GetByIdAsync(id, categoryId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting product {ProductId}, {CategoryId}", id, categoryId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetByCategoryId(int categoryId)
    {
        try
        {
            var products = await productService.GetByCategoryIdAsync(categoryId);
            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting products for category {CategoryId}", categoryId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] ProductCreateDto productDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            var createdProduct = await productService.CreateAsync(productDto);
            return CreatedAtAction(nameof(GetById), 
                new { id = createdProduct.Id, categoryId = createdProduct.CategoryId }, createdProduct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/{categoryId}")]
    public async Task<ActionResult<ProductResponseDto>> Update(string id, int categoryId, [FromBody] ProductUpdateDto productDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            var updatedProduct = await productService.UpdateAsync(id, categoryId, productDto);
            return Ok(updatedProduct);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating product {ProductId}, {CategoryId}", id, categoryId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}/{categoryId}")]
    public async Task<IActionResult> Delete(string id, int categoryId)
    {
        try
        {
            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            await productService.DeleteAsync(id, categoryId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting product {ProductId}, {CategoryId}", id, categoryId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private async Task<bool> IsAdmin()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring("Bearer ".Length).Trim();
            return await authService.IsAdminAsync(token);
        }
        catch
        {
            return false;
        }
    }
}
