using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CLTI.Diagnosis.Infrastructure.Services;

namespace CLTI.Diagnosis.Services
{

    public interface IUserService
    {
        Task<SysUser?> GetCurrentUserAsync(int userId);
        Task<bool> UpdateUserAsync(int userId, UpdateUserDto updateDto);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<bool> DeleteUserAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordHasherService _passwordHasher;

        public UserService(
            ApplicationDbContext context, 
            ILogger<UserService> logger,
            IPasswordHasherService passwordHasher)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task<SysUser?> GetCurrentUserAsync(int userId)
        {
            try
            {
                return await _context.SysUsers
                    .Include(u => u.StatusEnumItem)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR getting user | UserId: {UserId} | Error: {Error}", userId, ex.Message);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto updateDto)
        {
            try
            {
                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("👤 User not found for update | UserId: {UserId}", userId);
                    return false;
                }

                // Перевіряємо чи email не зайнятий іншим користувачем
                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
                {
                    var existingUser = await _context.SysUsers
                        .FirstOrDefaultAsync(u => u.Email == updateDto.Email && u.Id != userId);

                    if (existingUser != null)
                    {
                        _logger.LogWarning("📧 Email already taken by another user | Email: {Email}", updateDto.Email);
                        return false;
                    }
                }

                // Оновлюємо дані
                if (!string.IsNullOrEmpty(updateDto.FirstName))
                    user.FirstName = updateDto.FirstName;

                if (!string.IsNullOrEmpty(updateDto.LastName))
                    user.LastName = updateDto.LastName;

                if (!string.IsNullOrEmpty(updateDto.Email))
                    user.Email = updateDto.Email;

                await _context.SaveChangesAsync();
                _logger.LogDebug("User {UserId} updated successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR updating user | UserId: {UserId} | Error: {Error}", userId, ex.Message);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("👤 User not found for password change | UserId: {UserId}", userId);
                    return false;
                }

                // Перевіряємо старий пароль з автоматичною міграцією
                var (isValid, needsMigration) = _passwordHasher.VerifyPasswordWithMigration(
                    oldPassword, 
                    user.Password, 
                    user.PasswordHashType
                );

                if (!isValid)
                {
                    _logger.LogWarning("🔒 Invalid old password | UserId: {UserId}", userId);
                    return false;
                }

                // Встановлюємо новий пароль з використанням PBKDF2-SHA256 (сучасний стандарт)
                user.Password = _passwordHasher.HashPassword(newPassword);
                user.PasswordHashType = "PBKDF2-SHA256";
                
                await _context.SaveChangesAsync();

                // Password change - важлива подія для безпеки, логуємо як Warning щоб фіксувати в security logs
                _logger.LogWarning("🔑 Password changed successfully | UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR changing password | UserId: {UserId} | Error: {Error}", userId, ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("👤 User not found for deletion | UserId: {UserId}", userId);
                    return false;
                }

                _context.SysUsers.Remove(user);
                await _context.SaveChangesAsync();

                // User deletion - критична операція, логуємо як Warning
                _logger.LogWarning("🗑️ User deleted successfully | UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR deleting user | UserId: {UserId} | Error: {Error}", userId, ex.Message);
                return false;
            }
        }

    }

    // DTO для оновлення користувача
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}
