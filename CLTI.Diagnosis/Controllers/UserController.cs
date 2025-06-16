// ✅ UserController.cs - Тепер використовує JWT
using Microsoft.AspNetCore.Mvc;
using CLTI.Diagnosis.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ✅ Тепер використовує JWT за замовчуванням
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                _logger.LogInformation("GetCurrentUser called with JWT from {RemoteIp}",
                    HttpContext.Connection.RemoteIpAddress);

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found in JWT token" });
                }

                var user = await _userService.GetCurrentUserAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found in database", userId);
                    return NotFound(new { error = "User not found", userId });
                }

                _logger.LogInformation("Successfully retrieved user: {Email} (ID: {UserId})",
                    user.Email, user.Id);

                return Ok(new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    fullName = $"{user.FirstName} {user.LastName}".Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    details = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }

        [HttpGet("auth-test")]
        [AllowAnonymous] // ✅ Дозволяємо анонімний доступ для тестування
        public IActionResult TestAuthentication()
        {
            try
            {
                var authInfo = new
                {
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    Name = User.Identity?.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                    HasAuthorizationHeader = Request.Headers.ContainsKey("Authorization"),
                    AuthorizationHeader = Request.Headers.Authorization.ToString(),
                    RequestPath = HttpContext.Request.Path.Value,
                    Method = HttpContext.Request.Method,
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                    RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("JWT Auth test result: {@AuthInfo}", authInfo);
                return Ok(authInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auth test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found in JWT token" });
                }

                var updateDto = new UpdateUserDto
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email
                };

                var success = await _userService.UpdateUserAsync(userId, updateDto);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to update user. Email might be already taken." });
                }

                _logger.LogInformation("User {UserId} updated successfully", userId);
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found in JWT token" });
                }

                var success = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to change password. Old password might be incorrect." });
                }

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found in JWT token" });
                }

                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to delete user" });
                }

                _logger.LogInformation("User {UserId} deleted successfully", userId);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Request DTOs
    public class UpdateUserRequest
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
