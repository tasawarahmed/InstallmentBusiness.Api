namespace InstallmentBusiness.Api.DTOs;

public record LoginDto(string Username, string Password);

public record LoginResponseDto(string Token, DateTime ExpiresAt, string DisplayName);

// Requires an existing valid token to call -- there is no open/public
// self-signup. The first user is seeded automatically on first run
// (see Program.cs); every account after that is created by someone
// who is already logged in.
public record RegisterUserDto(string Username, string Password, string DisplayName);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);

public record UserResponseDto(int UserId, string Username, string DisplayName, bool IsActive);
