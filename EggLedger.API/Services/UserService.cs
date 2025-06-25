using EggLedger.API.Data;
using EggLedger.Core.DTOs.Auth;
using EggLedger.Core.DTOs.User;
using EggLedger.Core.Interfaces;
using EggLedger.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EggLedger.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        private static class UserRoles
        {
            public const int User = 0;
            public const int Admin = 1;
        }

        public UserService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _configuration = configuration;
        }

        public async Task<Result<List<UserSummaryDto>>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Result.Ok(users);
        }

        public async Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return Result.Fail("User not found");
            return Result.Ok(user);
        }

        public async Task<Result<UserSummaryDto>> CreateUserAsync(UserCreateDto dto)
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
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password),
                Role = dto.Role,
                Provider = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Result.Ok(new UserSummaryDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        public async Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Result.Fail("User not found");

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.Email != null && dto.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                    return Result.Fail("Email already exists");
                user.Email = dto.Email;
            }
            if (dto.Password != null) user.PasswordHash = _passwordHasher.HashPassword(null, dto.Password);
            if (dto.Role.HasValue) user.Role = dto.Role.Value;

            await _context.SaveChangesAsync();

            return Result.Ok(new UserSummaryDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        public async Task<Result> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return Result.Fail("User not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }

        public async Task<Result<TokenDto>> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // Fail if user doesn't exist OR if they signed up with Google (no password)
            if (user == null || user.PasswordHash == null)
                return Result.Fail("Invalid email or password.");

            var result = _passwordHasher.VerifyHashedPassword(null, user.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                return Result.Fail("Invalid email or password.");

            var tokenDto = GenerateJwtToken(user);
            return Result.Ok(tokenDto);
        }

        public async Task<Result<TokenDto>> LoginWithProviderAsync(string email, string name, string provider)
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
                    PasswordHash = null, // No password for social logins
                    Role = UserRoles.User, // Assign a default role
                    Provider = provider // Mark this user as created via "Google"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // At this point, we have a user. Generate a JWT for them.
            var tokenDto = GenerateJwtToken(user);
            return Result.Ok(tokenDto);
        }

        private TokenDto GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name), // Add the user's full name as a claim
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

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

            return new TokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expires = expiry
            };
        }
    }
}
