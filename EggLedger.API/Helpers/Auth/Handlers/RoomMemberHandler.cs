using System.Security.Claims;
using EggLedger.API.Helpers.Auth.Requirements;
using EggLedger.Data;
using EggLedger.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EggLedger.API.Helpers.Auth.Handlers;

public class RoomMemberHandler : AuthorizationHandler<RoomMemberRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RoomMemberHandler> _logger;

    public RoomMemberHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<RoomMemberHandler> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoomMemberRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("RoomMemberHandler: Invalid or missing user ID claim.");
            return;
        }

        var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
        var roomCodeStr = routeData?.Values["roomCode"]?.ToString();

        if (!int.TryParse(roomCodeStr, out var roomCode))
        {
            _logger.LogWarning("RoomMemberHandler: Invalid or missing roomCode in route.");
            return;
        }

        var isMember = await _context.UserRooms
            .AnyAsync(ur => ur.UserId == userId && ur.Room.RoomCode == roomCode && ur.Status == UserRoomStatus.Approved);

        if (isMember)
        {
            _logger.LogInformation("User '{UserId}' is a member of room '{RoomCode}'.", userId, roomCode);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Authorization failed: User '{UserId}' is not a member of room '{RoomCode}'.", userId, roomCode);
        }
    }
}
