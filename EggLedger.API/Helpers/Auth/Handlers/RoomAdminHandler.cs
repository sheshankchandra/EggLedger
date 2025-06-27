using EggLedger.API.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EggLedger.API.Helpers.Auth.Requirements;

namespace EggLedger.API.Helpers.Auth.Handlers
{    
    public class RoomAdminHandler : AuthorizationHandler<RoomAdminRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoomAdminHandler(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoomAdminRequirement requirement)
        {
            Guid userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            RouteData? routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            string? roomCodeStr = routeData?.Values["roomCode"]?.ToString();

            if (int.TryParse(roomCodeStr, out var roomCode))
            {
                var isAdmin = await _context.UserRooms
                    .AnyAsync(ur => ur.UserId == userId && ur.Room.RoomCode == roomCode && ur.IsAdmin);

                if (isAdmin)
                    context.Succeed(requirement);
            }
        }
    }
}
