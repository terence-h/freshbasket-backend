using Microsoft.Extensions.Options;
using Product.Service.Models.Configuration;
using Product.Service.Models.DTOs;
using System.Text.Json;

namespace Product.Service.Services;

public class AuthService(HttpClient httpClient, IOptions<AwsConfiguration> config, ILogger<AuthService> logger) : IAuthService
{
    private readonly AwsConfiguration _config = config.Value;

    public async Task<TokenValidationResponse?> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new TokenValidationRequest { Token = token };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_config.UserServiceBaseUrl}/api/token/validate", content);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Token validation failed with status: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenValidationResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    public async Task<bool> IsAdminAsync(string token)
    {
        try
        {
            var validation = await ValidateTokenAsync(token);
            return validation?.IsValid == true && validation.Roles.Contains("Admin");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking admin role");
            return false;
        }
    }
}
