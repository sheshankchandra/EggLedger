using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EggLedger.Data;
using EggLedger.DTO.Auth;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EggLedger.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IHelperService _helperService;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger, IConfiguration configuration, IHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
            _helperService = helperService;
        }

        public async Task<Result<TokenResponseDto>> CreateUserAsync(UserCreateDto dto)
        {
            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    return Result.Fail("Email already exists");

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Role = dto.Role,
                    Provider = null
                };

                string hashedPassword = _passwordHasher.HashPassword(user, dto.Password);

                var userPassword = new UserPassword()
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserId,
                    PasswordHash = hashedPassword
                };

                _context.Users.Add(user);
                _context.UserPasswords.Add(userPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.UserId, user.Email);

                return await CreateTokenResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in CreateUserAsync for email '{Email}'", dto.Email);
                return Result.Fail("An unexpected error occurred. Please try again later.");
            }
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

                var userPassword = await _context.UserPasswords
                    .FirstOrDefaultAsync(up => up.UserId == user.UserId);

                if (userPassword == null)
                {
                    _logger.LogWarning("This account was created using Google Sign-In. Please log in with Google");
                    return Result.Fail("Invalid Login Method");
                }

                PasswordVerificationResult result;
                try
                {
                    result = _passwordHasher.VerifyHashedPassword(user, userPassword.PasswordHash, dto.Password);
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
                _logger.LogError(ex, "Unexpected error occurred in LoginAsync for email '{Email}'", dto.Email);
                return Result.Fail("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<Result<TokenResponseDto>> LoginWithProviderAsync(string email, string name, string provider)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    _logger.LogWarning("OAuth login failed for email '{Email}': User not found, creating new user.", email);
                    _logger.LogInformation("Creating new user via OAuth with email: {Email}, name: '{Name}', provider: {Provider}", email, name, provider);

                    var nameParts = name?.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
                    var lastName = nameParts.Length > 1 ? nameParts[1] : null;

                    user = new User
                    {
                        UserId = Guid.NewGuid(),
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Role = UserRoles.User,
                        Provider = provider
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully created new OAuth user: {UserId}, Email: {Email}, Provider: {Provider}", user.UserId, email, provider);
                }

                _logger.LogInformation("User '{Email}' successfully logged in via {Provider}.", email, provider);
                return await CreateTokenResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in LoginWithProviderAsync for email '{Email}'", email);
                return Result.Fail("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<Result<TokenResponseDto>> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing refresh token request for user {UserId}", request.UserId);
                
                var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
                if (user is null)
                {
                    _logger.LogWarning("Invalid or expired refresh token for user {UserId}", request.UserId);
                    return Result.Fail("Invalid or Expired Refresh Token");
                }

                // Revoke the old refresh token
                await RevokeRefreshTokenAsync(request.UserId, request.RefreshToken);

                _logger.LogInformation("Refresh token successfully processed for user {UserId}", request.UserId);

                // Create new token pair
                return await CreateTokenResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in RefreshTokensAsync for userId {UserId}", request.UserId);
                return Result.Fail("An unexpected error occurred. Please try again later.");
            }
        }

        public async Task<Result<TokenResponseDto>> CreateTokenResponse(User? user)
        {
            try
            {
                var tokenResponse = new TokenResponseDto
                {
                    AccessToken = GenerateJwtToken(user ?? throw new ArgumentNullException(nameof(user))),
                    RefreshToken = await GenerateAndSaveRefreshTokenAsync(user)
                };

                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateTokenResponse for userId {UserId}", user?.UserId);
                return Result.Fail("An error occurred while creating token response.");
            }
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

            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException()));
            SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            DateTime expiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? throw new InvalidOperationException()));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: credentials
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
            try
            {
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    CreatedByIp = "TODO:CaptureIPAddress",
                    UserId = user.UserId,
                    Created = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Refresh token generated for user {UserId}", user.UserId);

                return refreshToken.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GenerateAndSaveRefreshTokenAsync for userId {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user is null) return null;

                var token = user.RefreshTokens
                    .FirstOrDefault(t => t.Token == refreshToken && !t.IsRevoked && !(t.Expires <= DateTime.UtcNow));

                return token != null ? user : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ValidateRefreshTokenAsync for userId {UserId}", userId);
                return null;
            }
        }

        public async Task RevokeRefreshTokenAsync(Guid userId, string refreshToken)
        {
            try
            {
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);

                if (token != null && !token.IsRevoked)
                {
                    token.Revoked = DateTime.UtcNow;
                    token.RevokedByIp = "TODO:CaptureIPAddress"; // You should capture the actual IP
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
                }
                else if (token != null && token.IsRevoked)
                {
                    _logger.LogWarning("Attempted to revoke already revoked token for user {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Attempted to revoke non-existent token for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RevokeRefreshTokenAsync for userId {UserId}", userId);
                throw;
            }
        }

        public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
        {
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(t => t.UserId == userId && !t.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.Revoked = DateTime.UtcNow;
                    token.RevokedByIp = "TODO:CaptureIPAddress";
                }

                if (tokens.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", tokens.Count, userId);
                }
                else
                {
                    _logger.LogInformation("No active refresh tokens found to revoke for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RevokeAllUserRefreshTokensAsync for userId {UserId}", userId);
                throw;
            }
        }

        public async Task CleanupExpiredRefreshTokensAsync()
        {
            try
            {
                var expiredTokens = await _context.RefreshTokens
                    .Where(t => t.Expires <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _context.RefreshTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CleanupExpiredRefreshTokensAsync");
                throw;
            }
        }

        public async Task<Result> LogoutAsync(Guid userId, string refreshToken)
        {
            try
            {
                await RevokeRefreshTokenAsync(userId, refreshToken);
                _logger.LogInformation("User {UserId} successfully logged out", userId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging out user {UserId}", userId);
                return Result.Fail("Logout failed");
            }
        }
    }
}
