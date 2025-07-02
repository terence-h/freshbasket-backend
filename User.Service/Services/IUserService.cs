namespace User.Service.Services;

using Models.DTOs;

public interface IUserService
{
    Task<UserResponseDto?> GetByIdAsync(string id);
    Task<UserResponseDto?> GetByEmailAsync(string email);
    Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
    Task<UserResponseDto> UpdateAsync(string id, Models.User user);
    Task DeleteAsync(string id);
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
}