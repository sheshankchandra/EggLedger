using EggLedger.Core.DTOs.Auth;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EggLedger.Core.Models;

namespace EggLedger.Core.Interfaces
{
    public interface IAuthService
    {
        Task<Result<TokenResponseDto>> LoginAsync(LoginDto dto);
        Task<Result<TokenResponseDto>> LoginWithProviderAsync(string email, string name, string provider);
        Task<Result<TokenResponseDto>> RefreshTokensAsync(RefreshTokenRequestDto request);
        Task<Result<TokenResponseDto>> CreateTokenResponse(User? user);
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        Task<string> GenerateAndSaveRefreshTokenAsync(User user);
        Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken);
        Task RevokeRefreshTokenAsync(Guid userId, string refreshToken);
        Task RevokeAllUserRefreshTokensAsync(Guid userId);
        Task CleanupExpiredRefreshTokensAsync();
        Task<Result> LogoutAsync(Guid userId, string refreshToken);
    }
}
