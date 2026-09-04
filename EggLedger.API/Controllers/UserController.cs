using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Controllers;

[ApiController]
[Route("egg-ledger-api/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
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
            return StatusCode(500, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetAllUsers");
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetAllUsers");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET: egg-ledger-api/user/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserSummaryDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Forbid();

        try
        {
            var result = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return StatusCode(500, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetUser, id: {Id}", id);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetUser for id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // PUT: egg-ledger-api/user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Forbid();

        // Only an Admin may change a role - otherwise a user could PUT their own
        // profile with a Role and promote themselves.
        if (dto.Role.HasValue && !User.IsInRole(nameof(UserRoles.Admin)))
            return Forbid();

        try
        {
            var result = await _userService.UpdateUserAsync(id, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for UpdateUser, id: {Id}", id);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in UpdateUser for id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/user/{id}/change-password
    // Self-only (not admin-on-behalf-of): requires knowledge of the current password.
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(callerId, out var parsedId) || parsedId != id)
            return Forbid();

        try
        {
            var result = await _userService.ChangePasswordAsync(id, dto, cancellationToken);
            if (result.IsSuccess)
                return Ok(new { message = "Password changed successfully" });
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for ChangePassword, id: {Id}", id);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ChangePassword for id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // DELETE: egg-ledger-api/user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(id))
            return Forbid();

        try
        {
            var result = await _userService.DeleteUserAsync(id, cancellationToken);
            if (result.IsSuccess)
                return Ok("User deleted successfully");
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for DeleteUser, id: {Id}", id);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DeleteUser for id: {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
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
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var result = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return NotFound(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetProfile");
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetProfile");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
