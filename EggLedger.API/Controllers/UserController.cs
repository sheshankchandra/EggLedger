using EggLedger.Core.DTOs.User;
using EggLedger.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<ActionResult<List<UserSummaryDto>>> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(500, result.Errors);
        }

        // GET: egg-ledger-api/user/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserSummaryDto>> GetUser(Guid id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return StatusCode(500, result.Errors);
        }

        // PUT: egg-ledger-api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto dto)
        {
            var result = await _userService.UpdateUserAsync(id, dto);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // DELETE: egg-ledger-api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (result.IsSuccess)
                return Ok("User deleted successfully");
            if (result.Errors.Any(e => e.Message == "User not found"))
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // DELETE: egg-ledger-api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get user ID from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var result = await _userService.GetUserByIdAsync(userId);
            if (result.IsSuccess)
                return Ok(result.Value);
            return NotFound(result.Errors.Select(e => e.Message));
        }

    }
}
