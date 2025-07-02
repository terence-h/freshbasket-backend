namespace User.Service.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Models.DTOs;
using Services;

[ApiController]
[Route("api/[controller]")]
public class TokenController(IJwtTokenService jwtTokenService, ILogger<TokenController> logger) : ControllerBase
{
    [HttpPost("validate")]
    public ActionResult<TokenValidationResponse> ValidateToken([FromBody] TokenValidationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
                return BadRequest(new { message = "Token is required" });

            var principal = jwtTokenService.ValidateToken(request.Token);
            if (principal == null)
                return Unauthorized(new { message = "Invalid or expired token" });

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new TokenValidationResponse
            {
                IsValid = true,
                UserId = userId ?? "",
                Email = email ?? "",
                Roles = roles
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
