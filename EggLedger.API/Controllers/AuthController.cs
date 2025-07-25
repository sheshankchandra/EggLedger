﻿using EggLedger.DTO.Auth;
using EggLedger.DTO.User;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EggLedger.Models.Options;
using Microsoft.Extensions.Configuration;

namespace EggLedger.API.Controllers;

[Route("egg-ledger-api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _configuration = configuration;
    }

    // POST: /egg-ledger-api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        try
        {
            var result = await _authService.CreateUserAsync(dto);
            if (result.IsSuccess)
                return Ok(result.Value);
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in CreateUser");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST /egg-ledger-api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", dto.Email);
            
            var result = await _authService.LoginAsync(dto);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful login for email: {Email}", dto.Email);
                return Ok(result.Value);
            }
            
            _logger.LogWarning("Failed login attempt for email: {Email}", dto.Email);
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in Login for email: {Email}", dto.Email);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET /egg-ledger-api/auth/google-login
    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        try
        {
            _logger.LogInformation("Google OAuth login initiated");
            var callbackUrl = Url.Action("GoogleCallback", "Auth", null, Request.Scheme);
            var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GoogleLogin");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET /egg-ledger-api/auth/google-callback
    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        try
        {
            var corsOptions = _configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
            var allowedOrigins = corsOptions.AllowedOrigins;
            if (!allowedOrigins.Any())
            {
                _logger.LogWarning("No allowed origins configured for CORS");
                return BadRequest("CORS configuration is missing allowed origins.");
            }
            // Try to get the Origin header from the request and Validate the origin
            var requestOrigin = Request.Headers["Origin"].FirstOrDefault() ?? allowedOrigins.FirstOrDefault();
            var redirectOrigin = allowedOrigins.Contains(requestOrigin) ? requestOrigin : allowedOrigins.FirstOrDefault();

            _logger.LogInformation("Processing Google OAuth callback");
            
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("Google authentication failed during callback");
                return BadRequest("Google authentication failed.");
            }

            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            var name = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Email claim not found in Google OAuth token");
                return BadRequest("Email claim not found in Google token.");
            }

            _logger.LogInformation("Processing OAuth login for email: {Email} with name: {Name}", email, name);

            // Use your user service to find or create the user and generate a JWT
            // This is the same logic you'd use after a successful password login.
            var loginResult = await _authService.LoginWithProviderAsync(email, name ?? throw new InvalidOperationException(), "Google");

            if (!loginResult.IsSuccess)
            {
                _logger.LogError("OAuth login failed for email: {Email}", email);
                // Redirect to the frontend login page with an error message
                var errorFrontendUrl = $"{redirectOrigin}/login?error=provider-login-failed";
                return Redirect(errorFrontendUrl);
            }

            _logger.LogInformation("OAuth login successful for email: {Email}, redirecting to frontend", email);

            // The user is successfully logged in, and we have a token.
            var token = loginResult.Value.AccessToken;
            var refreshToken = loginResult.Value.RefreshToken;
            var isNewRegistration = loginResult.Value.IsNewRegistration;

            // Redirect to your Vue app's callback component, passing the token
            var frontendCallbackUrl = $"{redirectOrigin}/auth/callback?token={token}&refreshToken={refreshToken}&isNewRegistration={isNewRegistration}";

            return Redirect(frontendCallbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GoogleCallback");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST /egg-ledger-api/auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        try
        {
            _logger.LogInformation("Refresh token request received for user {UserId}", request.UserId);
            
            var tokenResponse = await _authService.RefreshTokensAsync(request);
            if (tokenResponse.IsFailed || string.IsNullOrEmpty(tokenResponse.Value.AccessToken) || string.IsNullOrEmpty(tokenResponse.Value.RefreshToken))
            {
                _logger.LogWarning("Refresh token failed for user {UserId}", request.UserId);
                return Unauthorized("Invalid refresh token.");
            }

            _logger.LogInformation("Refresh token successful for user {UserId}", request.UserId);
            return Ok(tokenResponse.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in RefreshToken for userId: {UserId}", request.UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST /egg-ledger-api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        try
        {
            _logger.LogInformation("Received request to logout a user.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
            var result = await _authService.LogoutAsync(userId, refreshToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful logout for user: {userId}", userId);
                return Ok(new { message = "Logged out successfully" });
            }
            
            _logger.LogError("Failed logout attempt for user: {userId}", userId);
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in Logout for a user");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}