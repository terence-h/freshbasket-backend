using Order.Service.Models;

namespace Order.Service.Services;

public interface IUserService
{
    Task<TokenValidationResponse?> ValidateTokenAsync(string token);
    Task<string?> GetUserEmailAsync(string userId);
}