using EggLedger.Core.DTOs.User;
using FluentResults;

namespace EggLedger.Core.Interfaces
{
    public interface IUserService
    {
        Task<Result<List<UserSummaryDto>>> GetAllUsersAsync();
        Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id);
        Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto);
        Task<Result> DeleteUserAsync(Guid id);
    }
}
