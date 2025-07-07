using Microsoft.Extensions.Options;
using Order.Service.Models;
using System.Text.Json;
using Order.Service.DTOs;
using Order.Service.Models.Configurations;

namespace Order.Service.Services;

public class UserService(
    HttpClient httpClient,
    IOptions<AwsConfiguration> awsConfiguration,
    ILogger<UserService> logger)
    : IUserService
{
    private readonly AwsConfiguration awsConfiguration = awsConfiguration.Value;

    public async Task<TokenValidationResponse?> ValidateTokenAsync(string token)
    {
        try
        {
            var requestBody = new { token };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{awsConfiguration.UserServiceBaseUrl}/api/Token/validate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var validationResponse = JsonSerializer.Deserialize<TokenValidationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                logger.LogInformation("Token validation successful for user: {UserId}", validationResponse?.UserId);
                return validationResponse;
            }

            logger.LogWarning("Token validation failed with status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate token");
            return null;
        }
    }

    public async Task<string?> GetUserEmailAsync(string userId)
    {
        try
        {
            logger.LogInformation("Getting user email for userId: {UserId}", userId);
            
            var response = await httpClient.GetAsync($"{awsConfiguration.UserServiceBaseUrl}/api/Users/GetById?id={userId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<UserResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userResponse != null && !string.IsNullOrEmpty(userResponse.Email))
                {
                    logger.LogInformation("Successfully retrieved email for user: {UserId}", userId);
                    return userResponse.Email;
                }

                logger.LogWarning("User found but email is empty for userId: {UserId}", userId);
                return null;
            }

            logger.LogWarning("Failed to get user by ID. Status: {StatusCode}, UserId: {UserId}", response.StatusCode, userId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user email for userId: {UserId}", userId);
            return null;
        }
    }
}
