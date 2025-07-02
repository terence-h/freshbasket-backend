namespace User.Service.Models.DTOs;

public class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}