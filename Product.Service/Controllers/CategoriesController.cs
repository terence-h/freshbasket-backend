using Microsoft.AspNetCore.Mvc;
using Product.Service.Models.DTOs;
using Product.Service.Services;

namespace Product.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(
    ICategoryService categoryService,
    IAuthService authService,
    ILogger<CategoriesController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        try
        {
            var categories = await categoryService.GetAllAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all categories");
            return StatusCode(500, new { message = "Internal server error: Error getting all categories" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
    {
        try
        {
            var category = await categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found" });

            return Ok(category);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, new { message = $"Internal server error: Error getting category {id}" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CategoryCreateDto categoryDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            var createdCategory = await categoryService.CreateAsync(categoryDto);
            return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category");
            return StatusCode(500, new { message = "Internal server error: Error creating category" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(int id, [FromBody] CategoryUpdateDto categoryDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            var updatedCategory = await categoryService.UpdateAsync(id, categoryDto);
            return Ok(updatedCategory);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, new { message = $"Internal server error: Error updating category {id}" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            await categoryService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, new { message = $"Internal server error: Error deleting category {id}" });
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