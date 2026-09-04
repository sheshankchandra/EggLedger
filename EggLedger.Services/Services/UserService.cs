using EggLedger.Data;
using EggLedger.DTO.User;
using EggLedger.Models.Models;
using EggLedger.Services.Extensions;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services;

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

    public async Task<Result<List<UserSummaryDto>>> GetAllUsersAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var users = await _context.Users.AsNoTracking()
                .OrderBy(u => u.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(users);
        }, "An error occurred while retrieving users.");
    }

    public async Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
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
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", id);
                return Result.Fail("User not found");
            }
            return Result.Ok(user);
        }, "An error occurred while retrieving the user.");
    }

    public async Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
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
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id, cancellationToken))
                {
                    _logger.LogWarning("Attempted to update user {UserId} with existing email: {Email}", id, dto.Email);
                    return Result.Fail("Email already exists");
                }
                user.Email = dto.Email;
                emailChanged = true;
            }

            // Handle password update
            if (dto.Password != null)
            {
                var userPassword = await _context.UserPasswords
                    .FirstOrDefaultAsync(up => up.UserId == id, cancellationToken);

                if (userPassword != null)
                {
                    userPassword.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
                }
                else
                {
                    // Create new password record if it doesn't exist
                    userPassword = new UserPassword
                    {
                        Id = Guid.NewGuid(),
                        UserId = id,
                        PasswordHash = _passwordHasher.HashPassword(user, dto.Password)
                    };
                    _context.UserPasswords.Add(userPassword);
                }
            }

            if (dto.Role.HasValue) user.Role = dto.Role.Value;

            await _context.SaveChangesAsync(cancellationToken);

            if (emailChanged)
            {
                _logger.LogInformation("User updated successfully: {UserId}, Email changed", user.UserId);
            }
            else
            {
                _logger.LogInformation("User updated successfully: {UserId}", user.UserId);
            }

            return Result.Ok(new UserSummaryDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }, "An error occurred while updating the user.");
    }

    public async Task<Result> ChangePasswordAsync(Guid id, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempted to change password for non-existent user: {UserId}", id);
                return Result.Fail("User not found");
            }

            var userPassword = await _context.UserPasswords
                .FirstOrDefaultAsync(up => up.UserId == id, cancellationToken);

            if (userPassword == null)
            {
                _logger.LogWarning("Attempted to change password for Google-linked account: {UserId}", id);
                return Result.Fail("This account uses Google Sign-In and has no password to change.");
            }

            PasswordVerificationResult verification;
            try
            {
                verification = _passwordHasher.VerifyHashedPassword(user, userPassword.PasswordHash, dto.CurrentPassword);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Password verification failed while changing password for user {UserId}", id);
                return Result.Fail("Error occurred while verifying. Please try again later");
            }

            if (verification == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Password change failed for user {UserId}: current password incorrect.", id);
                return Result.Fail("Current password is incorrect");
            }

            userPassword.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password changed successfully for user {UserId}", id);
            return Result.Ok();
        }, "An error occurred while changing the password.");
    }

    public async Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _logger.ExecuteAsync(async () =>
        {
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Attempted to delete non-existent user: {UserId}", id);
                return Result.Fail("User not found");
            }

            var userEmail = user.Email;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User deleted successfully: {UserId}, Email: {Email}", id, userEmail);

            return Result.Ok();
        }, "An error occurred while deleting the user.");
    }
}
