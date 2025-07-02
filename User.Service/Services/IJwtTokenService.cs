namespace User.Service.Services;

using System.Security.Claims;

public interface IJwtTokenService
{
    string GenerateToken(Models.User user);
    ClaimsPrincipal? ValidateToken(string token);
}