using System.Security.Claims;
using EggLedger.API.Extensions;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers;

[ApiController]
[Route("egg-ledger-api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IStatsService _statsService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, IStatsService statsService, ILogger<UserController> logger)
    {
        _userService = userService;
        _statsService = statsService;
        _logger = logger;
    }

    /// <summary>
    /// True when the caller is either an Admin or the same user identified by <paramref name="userId"/>.
    /// Every non-Admin endpoint below is scoped to "your own account" with this check.
    /// </summary>
    private bool IsSelfOrAdmin(Guid userId)
    {
        if (User.IsInRole(nameof(UserRoles.Admin)))
            return true;

        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(callerId, out var parsedId) && parsedId == userId;
    }

    // GET: egg-ledger-api/user/all
    // Lists every user in the system - Admin only, never room-scoped or public.
    [HttpGet("all")]
    [Authorize(Roles = nameof(UserRoles.Admin))]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAllUsers([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 50 : pageSize;
            var result = await _userService.GetAllUsersAsync(effectivePage, effectivePageSize, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetAllUsers");
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetAllUsers");
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/user/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserSummaryDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Problem(detail: "You can only access your own account.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");

        try
        {
            var result = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetUser, id: {Id}", id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetUser for id: {Id}", id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // PUT: egg-ledger-api/user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Problem(detail: "You can only update your own account.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");

        // Only an Admin may change a role - otherwise a user could PUT their own
        // profile with a Role and promote themselves.
        if (dto.Role.HasValue && !User.IsInRole(nameof(UserRoles.Admin)))
            return Problem(detail: "Only an admin can change a user's role.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");

        try
        {
            var result = await _userService.UpdateUserAsync(id, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for UpdateUser, id: {Id}", id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in UpdateUser for id: {Id}", id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // POST: egg-ledger-api/user/{id}/change-password
    // Self-only (not admin-on-behalf-of): requires knowledge of the current password.
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(callerId, out var parsedId) || parsedId != id)
            return Problem(detail: "You can only change your own password.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");

        try
        {
            var result = await _userService.ChangePasswordAsync(id, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(new { message = "Password changed successfully" });
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for ChangePassword, id: {Id}", id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ChangePassword for id: {Id}", id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // DELETE: egg-ledger-api/user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Problem(detail: "You can only delete your own account.", statusCode: StatusCodes.Status403Forbidden, title: "Forbidden");

        try
        {
            var result = await _userService.DeleteUserAsync(id, cancellationToken);
            if (result.IsSuccess)
                return Ok("User deleted successfully");
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for DeleteUser, id: {Id}", id);
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DeleteUser for id: {Id}", id);
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/user/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Problem(detail: "Invalid user identity", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");

            var userId = Guid.Parse(userIdClaim.Value);

            var result = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetProfile");
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetProfile");
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }

    // GET: egg-ledger-api/user/stats?range=week|month|year|max
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] StatsRange range = StatsRange.Week, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Problem(detail: "Invalid user identity", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");

        var userId = Guid.Parse(userIdClaim.Value);

        try
        {
            var result = await _statsService.GetUserStatsAsync(userId, range, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return this.ToProblem(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetStats");
            return Problem(detail: "Client closed request.", statusCode: 499, title: "Request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetStats");
            return Problem(detail: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError, title: "Internal server error");
        }
    }
}
