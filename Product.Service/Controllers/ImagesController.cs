using Microsoft.AspNetCore.Mvc;
using Product.Service.Models.DTOs;
using Product.Service.Services;

namespace Product.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(IS3Service s3Service, IAuthService authService, ILogger<ImagesController> logger) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult<ImageUploadResponseDto>> Upload(IFormFile file)
    {
        try
        {
            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File is required" });

            var result = await s3Service.UploadImageAsync(file);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        try
        {
            // Simple admin check using Authorization header
            if (!await IsAdmin())
                return BadRequest(new { message = "Admin access required" });

            await s3Service.DeleteImageAsync(fileName);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting image {FileName}", fileName);
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
