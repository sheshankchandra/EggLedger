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
    [Route("api/[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // POST: api/join/{code}
        [HttpPost("join/")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomDto dto)
        {
            var result = await _roomService.JoinRoomAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result);
        }

        // POST: api/join/
        [HttpPost("create/")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var result = await _roomService.CreateRoomAsync(dto);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result);
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
