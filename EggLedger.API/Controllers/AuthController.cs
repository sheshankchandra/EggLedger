using System.Security.Claims;
using EggLedger.Core.DTOs.Auth;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers;

[Route("egg-ledger-api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result.IsSuccess)
            return Ok(result.Value);
        return Unauthorized(result.Errors.Select(e => e.Message));
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/egg-ledger-api/auth/google-callback" };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
            return BadRequest("Google authentication failed.");

        var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
        var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Email claim not found in Google token.");
        }

        // Use your user service to find or create the user and generate a JWT
        // This is the same logic you'd use after a successful password login.
        var loginResult = await _authService.LoginWithProviderAsync(email, name, "Google");

        if (!loginResult.IsSuccess)
        {
            // Redirect to the frontend login page with an error message
            var errorFrontendUrl = "http://localhost:5173/login?error=provider-login-failed";
            return Redirect(errorFrontendUrl);
        }

        // The user is successfully logged in, and we have a token.
        var token = loginResult.Value.AccessToken;

        // Redirect to your Vue app's callback component, passing the token
        var frontendCallbackUrl = $"http://localhost:5173/auth/callback?token={token}";

        return Redirect(frontendCallbackUrl);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        var tokenResponse = await _authService.RefreshTokensAsync(request);
        if (tokenResponse.IsFailed || string.IsNullOrEmpty(tokenResponse.Value.AccessToken) || string.IsNullOrEmpty(tokenResponse.Value.RefreshToken))
            return Unauthorized("Invalid refresh token.");

        return Ok(tokenResponse.Value);
    }
}