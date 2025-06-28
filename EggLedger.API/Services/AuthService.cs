using EggLedger.API.Data;
using EggLedger.Core.Constants;
using EggLedger.Core.DTOs.Auth;
using EggLedger.Core.Helpers;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EggLedger.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IHelperService _helperService;

        public AuthService(ApplicationDbContext context, ILogger<OrderService> logger, IConfiguration configuration, IHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
            _helperService = helperService;
        }

        public async Task<Result<TokenResponseDto>> LoginAsync(LoginDto dto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed for email '{Email}': User not found.", dto.Email);
                    return Result.Fail("Invalid email");
                }

                PasswordVerificationResult result;
                try
                {
                    result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                }
                catch (FormatException ex)
                {
                    _logger.LogError(ex, "Login failed for email '{Email}'", dto.Email);
                    return Result.Fail("Error occurred while verifying. Please try again later");
                }

                if (result != PasswordVerificationResult.Success)
                {
                    _logger.LogWarning("Login failed for email '{Email}': Incorrect password.", dto.Email);
                    return Result.Fail("Invalid email or password.");
                }

                _logger.LogInformation("User '{Email}' successfully logged in.", dto.Email);
                return await CreateTokenResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected error occurred in LoginAsync for email '{Email}'", dto.Email);
                return Result.Fail("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<Result<TokenResponseDto>> LoginWithProviderAsync(string email, string name, string provider)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                var nameParts = name?.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
                var lastName = nameParts.Length > 1 ? nameParts[1] : null;

                user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PasswordHash = null, 
                    Role = UserRoles.User,
                    Provider = provider
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return await CreateTokenResponse(user);
        }

        public async Task<Result<TokenResponseDto>> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
            if (user is null)
                return Result.Fail("Invalid or Expired Refresh Token");

            return await CreateTokenResponse(user);
        }

        public async Task<Result<TokenResponseDto>> CreateTokenResponse(User? user)
        {
            var tokenResponse =  new TokenResponseDto
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
            };

            return tokenResponse;
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name), // Add the user's full name as a claim
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // Add RoomId's to the claims
            var roomIds = _context.UserRooms
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.RoomId)
                .ToList();
            claims.AddRange(roomIds.Select(roomId => new Claim("Room", roomId.ToString())));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiryInMinutes"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedByIp = "TODO:CaptureIPAddress",
                UserId = user.UserId,
                Created = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken.Token;
        }

        public async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user is null) return null;

            var token = user.RefreshTokens
                .FirstOrDefault(t => t.Token == refreshToken && !t.IsRevoked && !(t.Expires <= DateTime.UtcNow));

            return token != null ? user : null;
        }
    }
}
