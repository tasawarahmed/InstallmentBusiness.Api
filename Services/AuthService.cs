using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InstallmentBusiness.Api.Data;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InstallmentBusiness.Api.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<UserResponseDto> RegisterAsync(RegisterUserDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<UserResponseDto> GetCurrentUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

        // Deliberately the same error for "no such user" and "wrong password" --
        // distinguishing them would tell an attacker which usernames exist.
        if (user is null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var (token, expiresAt) = GenerateToken(user);
        return new LoginResponseDto(token, expiresAt, user.DisplayName);
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Username and password are required.");

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            throw new ArgumentException($"Username '{dto.Username}' is already taken.");

        var user = new User
        {
            Username = dto.Username,
            DisplayName = dto.DisplayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserResponseDto(user.UserId, user.Username, user.DisplayName, user.IsActive);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();
    }

    public async Task<UserResponseDto> GetCurrentUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return new UserResponseDto(user.UserId, user.Username, user.DisplayName, user.IsActive);
    }

    private (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim("userId", user.UserId.ToString()),
            new Claim("displayName", user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
