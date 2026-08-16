using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EggLedger.API.Extensions;
using EggLedger.DTO.Auth;
using EggLedger.DTO.User;
using EggLedger.Models.Options;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Controllers;

[Route("egg-ledger-api/auth")]
[ApiController]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "eggledger_refresh_token";
    private const string RefreshCookiePath = "/egg-ledger-api/auth";
    // Cookie-authenticated endpoints require this custom header. Browsers force a
    // CORS preflight for it, and our CORS allows only our own origins, so a
    // cross-site page cannot supply it -> CSRF protection for the SameSite=None cookie.
    private const string CsrfHeaderName = "X-EggLedger-CSRF";
    private static readonly TimeSpan RefreshCookieLifetime = TimeSpan.FromDays(7);

    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _configuration = configuration;
    }

    // Builds the flags for the refresh-token cookie.
    // HttpOnly  -> JavaScript (and therefore XSS) can never read it.
    // Secure    -> only sent over HTTPS. The API runs HTTPS in dev (Aspire) and prod, so always on.
    // SameSite  -> None because the SPA (Static Web Apps) and API (Container Apps) live on different domains.
    // Path      -> scoped to /auth so the cookie rides along only with refresh/logout, not every API call.
    private static CookieOptions BuildRefreshCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = RefreshCookiePath,
        Expires = expires,
        IsEssential = true
    };

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var expires = DateTimeOffset.UtcNow.Add(RefreshCookieLifetime);
        Response.Cookies.Append(RefreshCookieName, refreshToken, BuildRefreshCookieOptions(expires));
    }

    private void ClearRefreshTokenCookie()
    {
        // Delete must use the same Path/flags the cookie was written with, or the browser keeps it.
        Response.Cookies.Append(RefreshCookieName, string.Empty, BuildRefreshCookieOptions(DateTimeOffset.UnixEpoch));
    }

    // Anti-CSRF: cookie-authenticated endpoints require the custom header. A
    // cross-site attacker cannot send it (CORS preflight blocks foreign origins).
    private bool IsMissingCsrfHeader() => !Request.Headers.ContainsKey(CsrfHeaderName);

    // POST: /egg-ledger-api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        try
        {
            var result = await _authService.CreateUserAsync(dto);
            if (result.IsSuccess)
            {
                SetRefreshTokenCookie(result.Value.RefreshToken);
                return Ok(new { accessToken = result.Value.AccessToken, isNewRegistration = result.Value.IsNewRegistration });
            }
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
                SetRefreshTokenCookie(result.Value.RefreshToken);
                return Ok(new { accessToken = result.Value.AccessToken });
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
            if (allowedOrigins.Length == 0)
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

            // Store the refresh token in an HttpOnly cookie instead of the URL.
            // The SPA never sees the refresh token; it calls /auth/refresh to obtain an in-memory access token.
            SetRefreshTokenCookie(loginResult.Value.RefreshToken);

            var isNewRegistration = loginResult.Value.IsNewRegistration;

            // Redirect to the Vue callback route with NO tokens in the URL (only a non-sensitive flag).
            var frontendCallbackUrl = $"{redirectOrigin}/auth/callback?isNewRegistration={isNewRegistration}";

            return Redirect(frontendCallbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GoogleCallback");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST /egg-ledger-api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            if (IsMissingCsrfHeader())
            {
                _logger.LogWarning("Refresh rejected: missing CSRF header");
                return StatusCode(StatusCodes.Status403Forbidden, "Missing required header.");
            }

            var refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogInformation("Refresh request with no refresh-token cookie");
                return Unauthorized("No refresh token.");
            }

            var tokenResponse = await _authService.RefreshTokensAsync(refreshToken);
            if (tokenResponse.IsFailed || string.IsNullOrEmpty(tokenResponse.Value.AccessToken) || string.IsNullOrEmpty(tokenResponse.Value.RefreshToken))
            {
                _logger.LogWarning("Refresh token rejected");
                ClearRefreshTokenCookie();
                return Unauthorized("Invalid refresh token.");
            }

            // Rotation: replace the cookie with the newly issued refresh token.
            SetRefreshTokenCookie(tokenResponse.Value.RefreshToken);
            return Ok(new { accessToken = tokenResponse.Value.AccessToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in RefreshToken");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST /egg-ledger-api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            if (IsMissingCsrfHeader())
            {
                _logger.LogWarning("Logout rejected: missing CSRF header");
                return StatusCode(StatusCodes.Status403Forbidden, "Missing required header.");
            }

            _logger.LogInformation("Received request to logout a user.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
            var refreshToken = Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(userId, refreshToken);
            }

            ClearRefreshTokenCookie();
            _logger.LogInformation("Successful logout for user: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in Logout for a user");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}