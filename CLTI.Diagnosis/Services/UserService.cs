using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
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
                _logger.LogError(ex, "Error getting user {UserId}", userId);
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
                    _logger.LogWarning("User {UserId} not found for update", userId);
                    return false;
                }

                // Перевіряємо чи email не зайнятий іншим користувачем
                if (!string.IsNullOrEmpty(updateDto.Email) && updateDto.Email != user.Email)
                {
                    var existingUser = await _context.SysUsers
                        .FirstOrDefaultAsync(u => u.Email == updateDto.Email && u.Id != userId);

                    if (existingUser != null)
                    {
                        _logger.LogWarning("Email {Email} already taken by another user", updateDto.Email);
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
                _logger.LogInformation("User {UserId} updated successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
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
                    _logger.LogWarning("User {UserId} not found for password change", userId);
                    return false;
                }

                // Перевіряємо старий пароль
                var oldPasswordHash = HashPassword(oldPassword);
                if (user.Password != oldPasswordHash)
                {
                    _logger.LogWarning("Invalid old password for user {UserId}", userId);
                    return false;
                }

                // Встановлюємо новий пароль
                user.Password = HashPassword(newPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
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
                    _logger.LogWarning("User {UserId} not found for deletion", userId);
                    return false;
                }

                _context.SysUsers.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
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