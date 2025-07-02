namespace User.Service.Services;

using Extensions;
using Models.Configurations;
using Models.DTOs;
using Repositories;
using Models;

using Microsoft.Extensions.Options;
using BC = BCrypt.Net.BCrypt;

public class UserService(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    ILogger<UserService> logger,
    IOptions<AwsConfiguration> config)
    : IUserService
{
    private readonly ILogger<UserService> _logger = logger;
    private readonly AwsConfiguration _config = config.Value;

    public async Task<UserResponseDto?> GetByIdAsync(string id)
    {
        var user = await userRepository.GetByIdAsync(id);
        return user?.ToResponseDto();
    }

    public async Task<UserResponseDto?> GetByEmailAsync(string email)
    {
        var user = await userRepository.GetByEmailAsync(email);
        return user?.ToResponseDto();
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Check if user already exists
        var existingUser = await userRepository.GetByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create new user
        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = BC.HashPassword(registerDto.Password),
            Roles = registerDto.Roles.Any() ? registerDto.Roles : new List<string> { "User" }
        };

        var createdUser = await userRepository.CreateAsync(user);
        var token = jwtTokenService.GenerateToken(createdUser);
        var expiresAt = DateTime.UtcNow.AddMinutes(_config.JwtExpirationMinutes);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = createdUser.ToResponseDto()
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null || !BC.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is deactivated");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user);

        var token = jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_config.JwtExpirationMinutes);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = user.ToResponseDto()
        };
    }

    public async Task<UserResponseDto> UpdateAsync(string id, User user)
    {
        var existingUser = await userRepository.GetByIdAsync(id);
        if (existingUser == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.Id = id;
        var updatedUser = await userRepository.UpdateAsync(user);
        return updatedUser.ToResponseDto();
    }

    public async Task DeleteAsync(string id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        await userRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(u => u.ToResponseDto());
    }
}