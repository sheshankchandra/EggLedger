using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EggLedger.DTO.User;
using EggLedger.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Controllers
{
    [ApiController]
    [Route("egg-ledger-api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: egg-ledger-api/user/all
        [HttpGet("all")]
        public async Task<ActionResult<List<UserSummaryDto>>> GetAllUsers(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _userService.GetAllUsersAsync(cancellationToken);
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

        // DELETE: egg-ledger-api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
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
}
