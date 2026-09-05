using System.Security.Claims;
using EggLedger.DTO.Room;
using EggLedger.DTO.User;
using EggLedger.Services.Errors;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EggLedger.API.Controllers;

[ApiController]
[Route("egg-ledger-api/room")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IRoomService roomService, ILogger<RoomController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    // POST: egg-ledger-api/join/{code}
    [HttpPost("join/{roomCode:int}")]
    [Authorize]
    public async Task<IActionResult> JoinRoom([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received request to join a Room.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user identity");
            }

            Result<JoinRoomResultDto> result = await _roomService.JoinRoomAsync(userId, roomCode, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("User successfully joined room with code: {RoomCode}", roomCode);
                return Ok(result);
            }

            _logger.LogWarning("Failed to join room. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for JoinRoom, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in JoinRoom for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/create
    [HttpPost("create/")]
    [Authorize]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received request to create a Room.");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user identity");
            }

            Result<int> result = await _roomService.CreateRoomAsync(userId, dto, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created the Room : {RoomCode}", result.Value);
                return Ok(result);
            }

            _logger.LogWarning("Failed to create room. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for CreateRoom");
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in CreateRoom");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/
    [HttpGet("{roomCode:int}")]
    public async Task<ActionResult<List<UserSummaryDto>>> GetRoomByCode([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roomService.GetRoomByCodeAsync(roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(500, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetRoomByCode, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetRoomByCode for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/all
    [HttpGet("{roomCode:int}/users")]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAllRoomUsers([FromRoute] int roomCode, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 50 : pageSize;
            var result = await _roomService.GetAllRoomUsersAsync(roomCode, effectivePage, effectivePageSize, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(500, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetAllRoomUsers, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetAllRoomUsers for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET: egg-ledger-api/room/user/all
    [HttpGet("user/all")]
    [Authorize]
    public async Task<ActionResult<List<RoomDto>>> GetAllUserRooms(CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user identity");
            }

            var result = await _roomService.GetAllUserRoomsAsync(userId, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            return StatusCode(500, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetAllUserRooms");
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetAllUserRooms");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/update/
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("update/IsPublic")]
    public async Task<IActionResult> UpdateRoomIsPublicStatus([FromBody] UpdateRoomPublicStatusDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _roomService.UpdateRoomPublicStatusAsync(dto, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Reasons);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for UpdateRoomIsPublicStatus");
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in UpdateRoomIsPublicStatus");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/delete/{roomCode}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("delete/{roomCode:int}")]
    public async Task<IActionResult> DeleteRoom([FromRoute] int roomCode, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Received request to delete room with code: {RoomCode}", roomCode);

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Delete room request denied - Invalid user identity for room code: {RoomCode}", roomCode);
                return Unauthorized("Invalid user identity");
            }

            var result = await _roomService.DeleteRoomAsync(roomCode, userId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully deleted room with code: {RoomCode} by user: {UserId}", roomCode, userId);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to delete room with code: {RoomCode}. Errors: {Errors}", roomCode, string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for DeleteRoom, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in DeleteRoom for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/edit-name/{roomCode}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("edit-name/{roomCode:int}")]
    public async Task<IActionResult> EditRoomName([FromBody] EditRoomNameDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user identity");
            }

            var result = await _roomService.EditRoomNameAsync(userId, dto.RoomId, dto.NewRoomName, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Room name updated for RoomId: {RoomId} by User: {UserId}", dto.RoomId, userId);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to edit room name. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for EditRoomName, RoomId: {RoomId}", dto.RoomId);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in EditRoomName for RoomId: {RoomId}", dto.RoomId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/remove-member/{roomCode}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("remove-member/{roomCode:int}")]
    public async Task<IActionResult> RemoveRoomMember([FromBody] RemoveRoomMemberDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserIdClaim) || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
            {
                return Unauthorized("Invalid user identity");
            }

            var result = await _roomService.RemoveRoomMemberAsync(adminUserId, dto.RoomId, dto.MemberUserId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Member {MemberUserId} removed from RoomId: {RoomId} by Admin: {AdminUserId}", dto.MemberUserId, dto.RoomId, adminUserId);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to remove room member. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for RemoveRoomMember, RoomId: {RoomId}", dto.RoomId);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in RemoveRoomMember for RoomId: {RoomId}", dto.RoomId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/edit-status/{roomCode}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("edit-status/{roomCode:int}")]
    public async Task<IActionResult> EditRoomStatus([FromBody] EditRoomStatusDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user identity");
            }

            var result = await _roomService.EditRoomStatusAsync(userId, dto.RoomId, dto.NewStatus, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Room status updated for RoomId: {RoomId} by User: {UserId}", dto.RoomId, userId);
                return Ok(result.Value);
            }

            _logger.LogWarning("Failed to edit room status. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for EditRoomStatus, RoomId: {RoomId}", dto.RoomId);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in EditRoomStatus for RoomId: {RoomId}", dto.RoomId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // GET: egg-ledger-api/room/{roomCode}/pending-members
    [Authorize(Policy = "RoomAdmin")]
    [HttpGet("{roomCode:int}/pending-members")]
    public async Task<IActionResult> GetPendingMembers([FromRoute] int roomCode, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity");
        }

        try
        {
            var result = await _roomService.GetPendingMembersAsync(userId, roomCode, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.HasError<NotFoundError>())
                return NotFound();
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetPendingMembers, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetPendingMembers for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/{roomCode}/approve-member/{memberUserId}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("{roomCode:int}/approve-member/{memberUserId:guid}")]
    public async Task<IActionResult> ApproveMember([FromRoute] int roomCode, [FromRoute] Guid memberUserId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity");
        }

        try
        {
            var result = await _roomService.ApproveMemberAsync(userId, roomCode, memberUserId, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Member {MemberUserId} approved into room {RoomCode} by {UserId}", memberUserId, roomCode, userId);
                return Ok(result.Value);
            }
            if (result.HasError<NotFoundError>())
                return NotFound(result.Errors.Select(e => e.Message));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for ApproveMember, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in ApproveMember for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // POST: egg-ledger-api/room/{roomCode}/reject-member/{memberUserId}
    [Authorize(Policy = "RoomAdmin")]
    [HttpPost("{roomCode:int}/reject-member/{memberUserId:guid}")]
    public async Task<IActionResult> RejectMember([FromRoute] int roomCode, [FromRoute] Guid memberUserId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user identity");
        }

        try
        {
            var result = await _roomService.RejectMemberAsync(userId, roomCode, memberUserId, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Member {MemberUserId} rejected from room {RoomCode} by {UserId}", memberUserId, roomCode, userId);
                return Ok(result.Value);
            }
            if (result.HasError<NotFoundError>())
                return NotFound(result.Errors.Select(e => e.Message));
            return BadRequest(result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for RejectMember, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in RejectMember for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
