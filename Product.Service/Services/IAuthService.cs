using Product.Service.Models.DTOs;

namespace Product.Service.Services;

public interface IAuthService
{
    Task<TokenValidationResponse?> ValidateTokenAsync(string token);
    Task<bool> IsAdminAsync(string token);
}