using EggLedger.Data;
using EggLedger.DTO.User;
using EggLedger.Models.Models;
using EggLedger.Services.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EggLedger.Services.Services
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

        public async Task<Result<List<UserSummaryDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _context.Users.AsNoTracking()
                    .Select(u => new UserSummaryDto
                    {
                        UserId = u.UserId,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role
                    })
                    .ToListAsync(cancellationToken);

                return Result.Ok(users);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetAllUsersAsync was canceled");
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllUsersAsync");
                return Result.Fail("An error occurred while retrieving users.");
            }
        }

        public async Task<Result<UserSummaryDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
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
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "GetUserByIdAsync was canceled for userId {UserId}", id);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUserByIdAsync for userId {UserId}", id);
                return Result.Fail("An error occurred while retrieving the user.");
            }
        }

        public async Task<Result<UserSummaryDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
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
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "UpdateUserAsync was canceled for userId {UserId}", id);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateUserAsync for userId {UserId}", id);
                return Result.Fail("An error occurred while updating the user.");
            }
        }

        public async Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
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
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "DeleteUserAsync was canceled for userId {UserId}", id);
                return Result.Fail("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteUserAsync for userId {UserId}", id);
                return Result.Fail("An error occurred while deleting the user.");
            }
        }
    }
}
