using System;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EggLedger.API.Helpers.Auth.Requirements;
using EggLedger.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace EggLedger.API.Helpers.Auth.Handlers
{    
    public class RoomAdminHandler : AuthorizationHandler<RoomAdminRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RoomAdminHandler> _logger;

        public RoomAdminHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<RoomAdminHandler> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoomAdminRequirement requirement)
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
                .AnyAsync(ur => ur.UserId == userId && ur.Room.Code == roomCode && ur.IsAdmin);

            if (isMember)
            {
                _logger.LogInformation("User '{Id}' is a admin of room '{Code}'.", userId, roomCode);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Authorization failed: User '{Id}' is not a admin of room '{Code}'.", userId, roomCode);
            }
        }
    }
}
