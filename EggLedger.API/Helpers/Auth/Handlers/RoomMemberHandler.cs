using EggLedger.API.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EggLedger.API.Helpers.Auth.Requirements;

namespace EggLedger.API.Helpers.Auth.Handlers
{
    public class RoomMemberHandler : AuthorizationHandler<RoomMemberRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoomMemberHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoomMemberRequirement requirement)
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var roomCodeStr = routeData?.Values["roomCode"]?.ToString();

            if (int.TryParse(roomCodeStr, out var roomCode))
            {
                var isMember = await _context.UserRooms
                    .AnyAsync(ur => ur.UserId == userId && ur.Room.RoomCode == roomCode);

                if (isMember)
                    context.Succeed(requirement);
            }
        }
    }
}
