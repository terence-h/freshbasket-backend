using User.Service.Models.DTOs;

namespace User.Service.Extensions;

public static class UserExtensions
{
    public static UserResponseDto ToResponseDto(this Models.User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Roles = user.Roles,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
}