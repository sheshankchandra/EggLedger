using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EggLedger.Data;
using EggLedger.DTO.Auth;
using EggLedger.DTO.User;
using EggLedger.Models.Enums;
using EggLedger.Models.Models;
using EggLedger.Models.Options;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EggLedger.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly JwtOptions _jwtOptions;
        private readonly IHelperService _helperService;

        public AuthService(
            ApplicationDbContext context, 
            ILogger<AuthService> logger, 
            IOptions<JwtOptions> jwtOptions, 
            IHelperService helperService)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
            _jwtOptions = jwtOptions.Value;
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
                    Id = Guid.NewGuid(),
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
                    UserId = user.Id,
                    PasswordHash = hashedPassword
                };

                _context.Users.Add(user);
                _context.UserPasswords.Add(userPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {Id}, Email: {Email}", user.Id, user.Email);

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
                    .FirstOrDefaultAsync(up => up.UserId == user.Id);

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
                var isNewUser = false;

                if (user == null)
                {
                    _logger.LogWarning("OAuth login failed for email '{Email}': User not found, creating new user.", email);
                    _logger.LogInformation("Creating new user via OAuth with email: {Email}, name: '{Name}', provider: {Provider}", email, name, provider);

                    var nameParts = name?.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries) ?? [];
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
                    var lastName = nameParts.Length > 1 ? nameParts[1] : null;

                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Role = UserRoles.User,
                        Provider = provider
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    isNewUser = true;
                    _logger.LogInformation("Successfully created new OAuth user: {Id}, Email: {Email}, Provider: {Provider}", user.Id, email, provider);
                }

                _logger.LogInformation("User '{Email}' successfully logged in via {Provider}.", email, provider);
                var tokenResponse = await CreateTokenResponse(user);
                
                if (isNewUser)
                {
                    tokenResponse.Value.IsNewRegistration = true;
                }
                
                return tokenResponse;
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
                _logger.LogInformation("Processing refresh token request for user {Id}", request.UserId);
                
                var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);
                if (user is null)
                {
                    _logger.LogWarning("Invalid or expired refresh token for user {Id}", request.UserId);
                    return Result.Fail("Invalid or Expired Refresh Token");
                }

                // Revoke the old refresh token
                await RevokeRefreshTokenAsync(request.UserId, request.RefreshToken);

                _logger.LogInformation("Refresh token successfully processed for user {Id}", request.UserId);

                // Create new token pair
                return await CreateTokenResponse(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in RefreshTokensAsync for userId {Id}", request.UserId);
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
                _logger.LogError(ex, "Error occurred in CreateTokenResponse for userId {Id}", user?.Id);
                return Result.Fail("An error occurred while creating token response.");
            }
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name), // Add the user's full name as a claim
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // Add Id's to the claims
            var roomIds = _context.UserRooms
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoomId)
                .ToList();
            claims.AddRange(roomIds.Select(roomId => new Claim("Room", roomId.ToString())));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
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
            try
            {
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = GenerateRefreshToken(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    CreatedByIp = "TODO:CaptureIPAddress",
                    UserId = user.Id,
                    Created = DateTime.UtcNow
                };

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refresh token generated for user {Id}", user.Id);

                return refreshToken.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GenerateAndSaveRefreshTokenAsync for userId {Id}", user.Id);
                throw;
            }
        }

        public async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user is null) return null;

                var token = user.RefreshTokens
                    .FirstOrDefault(t => t.Token == refreshToken && !t.IsRevoked && !(t.Expires <= DateTime.UtcNow));

                return token != null ? user : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ValidateRefreshTokenAsync for userId {Id}", userId);
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
                    // TODO:CaptureIPAddress
                    token.RevokedByIp = "Not Found";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Refresh token revoked for user {Id}", userId);
                }
                else if (token != null && token.IsRevoked)
                {
                    _logger.LogWarning("Attempted to revoke already revoked token for user {Id}", userId);
                }
                else
                {
                    _logger.LogWarning("Attempted to revoke non-existent token for user {Id}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RevokeRefreshTokenAsync for userId {Id}", userId);
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
                    _logger.LogInformation("Revoked {Count} refresh tokens for user {Id}", tokens.Count, userId);
                }
                else
                {
                    _logger.LogInformation("No active refresh tokens found to revoke for user {Id}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RevokeAllUserRefreshTokensAsync for userId {Id}", userId);
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
                _logger.LogInformation("User {Id} successfully logged out", userId);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging out user {Id}", userId);
                return Result.Fail("Logout failed");
            }
        }
    }
}
