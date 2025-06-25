using EggLedger.Core.DTOs.Auth;
using EggLedger.Core.DTOs.Order;
using EggLedger.Core.DTOs.User;
using EggLedger.Core.Models;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggLedger.Core.Interfaces
{
    public interface IUserService
    {
        Task<Result<List<UserSummaryDto>>> GetAllUsersAsync();
        Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id);
        Task<Result<UserSummaryDto>> CreateUserAsync(UserCreateDto dto);
        Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto);
        Task<Result> DeleteUserAsync(Guid id);
        Task<Result<TokenDto>> LoginAsync(LoginDto dto);
        Task<Result<TokenDto>> LoginWithProviderAsync(string email, string name, string provider);
    }
}
