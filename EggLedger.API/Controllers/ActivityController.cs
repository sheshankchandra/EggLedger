using EggLedger.Services.Errors;
using EggLedger.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Controllers;

// Requires the caller to be a member of the room named in the route - see RoomMemberHandler,
// which reads {roomCode} straight off the route data.
[ApiController]
[Route("egg-ledger-api/room/{roomCode:int}/activity")]
[Authorize(Policy = "RoomMember")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(IActivityService activityService, ILogger<ActivityController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    // GET: egg-ledger-api/room/{roomCode}/activity?page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> GetActivity([FromRoute] int roomCode, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = pageSize <= 0 ? 30 : pageSize;
            var result = await _activityService.GetRoomActivityAsync(roomCode, effectivePage, effectivePageSize, cancellationToken);
            if (result.IsSuccess)
                return Ok(result.Value);
            if (result.HasError<NotFoundError>())
                return NotFound();
            return StatusCode(500, result.Errors.Select(e => e.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was canceled by the client for GetActivity, roomCode: {RoomCode}", roomCode);
            return StatusCode(499, "Client closed request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GetActivity for roomCode: {RoomCode}", roomCode);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
