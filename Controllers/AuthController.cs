using System.Security.Claims;
using InstallmentBusiness.Api.DTOs;
using InstallmentBusiness.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstallmentBusiness.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    private int CurrentUserId => int.Parse(User.FindFirstValue("userId")!);

    // The only endpoint in the whole API that doesn't require a token.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto) =>
        await _auth.LoginAsync(dto);

    // Requires an existing valid token -- there is no public self-signup.
    // The first account is seeded automatically on first run (see Program.cs);
    // every account after that is created by someone already logged in.
    [HttpPost("register")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> Register(RegisterUserDto dto) =>
        await _auth.RegisterAsync(dto);

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        await _auth.ChangePasswordAsync(CurrentUserId, dto);
        return NoContent();
    }

    // Lets the frontend confirm a stored token is still valid and fetch the
    // display name for "logged in as ..." without decoding the JWT client-side.
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> Me() =>
        await _auth.GetCurrentUserAsync(CurrentUserId);
}
