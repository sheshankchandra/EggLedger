using EggLedger.Core.DTOs.Room;
using EggLedger.Core.DTOs.User;
using EggLedger.Core.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EggLedger.API.Controllers
{
    [ApiController]
    [Route("egg-ledger-api/[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ILogger<OrderController> _logger;

        public RoomController(IRoomService roomService, ILogger<OrderController> logger)
        {
            _roomService = roomService;
            _logger = logger;
        }

        // POST: api/join/{code}
        [HttpPost("join/")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomDto dto)
        {
            var result = await _roomService.JoinRoomAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result);
        }

        // POST: api/room/create
        [HttpPost("create/")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            _logger.LogInformation("Received request to create a Room.");

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var result = await _roomService.CreateRoomAsync(userId, dto);

            if (result is { IsSuccess: true, Value: not null })
            {
                _logger.LogInformation("Successfully created the Room : {OrderName}", result.Value);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to create room. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }

        // POST: api/update/
        [Authorize(Policy = "RoomAdmin")]
        [HttpPost("update/IsPublic")]
        public async Task<IActionResult> UpdateRoomIsPublicStatus([FromBody] UpdateRoomPublicStatusDto dto)
        {
            var result = await _roomService.UpdateRoomPublicStatusAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Reasons);
        }
    }
}
