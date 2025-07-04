using EggLedger.DTO.User;
using FluentResults;

namespace EggLedger.Services.Interfaces
{
    public interface IUserService
    {
        Task<Result<List<UserSummaryDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default);
        Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
