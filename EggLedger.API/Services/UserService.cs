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
        private readonly ILogger<UserService> _logger;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<Result<List<UserSummaryDto>>> GetAllUsersAsync()
        {
            var users = await _context.Users.AsNoTracking()
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
            _logger.LogDebug("Retrieving user with ID: {UserId}", id);
            
            var user = await _context.Users.AsNoTracking()
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
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return Result.Fail("User not found");
            }
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

            _logger.LogInformation("User created successfully: {UserId}, Email: {Email}", user.UserId, user.Email);

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
            {
                _logger.LogWarning("Attempted to update non-existent user: {UserId}", id);
                return Result.Fail("User not found");
            }

            var originalEmail = user.Email;
            bool emailChanged = false;

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.Email != null && dto.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                {
                    _logger.LogWarning("Attempted to update user {UserId} with existing email: {Email}", id, dto.Email);
                    return Result.Fail("Email already exists");
                }
                user.Email = dto.Email;
                emailChanged = true;
            }
            if (dto.Password != null) user.PasswordHash = _passwordHasher.HashPassword(null, dto.Password);
            if (dto.Role.HasValue) user.Role = dto.Role.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated successfully: {UserId}" + (emailChanged ? ", Email changed from {OldEmail} to {NewEmail}" : ""), 
                user.UserId, emailChanged ? originalEmail : null, emailChanged ? user.Email : null);

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
            {
                _logger.LogWarning("Attempted to delete non-existent user: {UserId}", id);
                return Result.Fail("User not found");
            }

            var userEmail = user.Email;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted successfully: {UserId}, Email: {Email}", id, userEmail);

            return Result.Ok();
        }
    }
}
