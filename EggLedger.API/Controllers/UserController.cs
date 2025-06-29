using EggLedger.Core.DTOs.User;
using EggLedger.Core.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EggLedger.API.Controllers
{
    [ApiController]
    [Route("egg-ledger-api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/user/all
        [HttpGet("all")]
        public async Task<ActionResult<List<UserSummaryDto>>> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(500, result.Errors);
        }

        // GET: api/user/{id}
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

        // POST: api/user
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            var result = await _userService.CreateUserAsync(dto);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetUser), new { id = result.Value.UserId }, result.Value);
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // PUT: api/user/{id}
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

        // DELETE: api/user/{id}
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

        // DELETE: api/user/profile
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
