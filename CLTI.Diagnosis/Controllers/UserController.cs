using Microsoft.AspNetCore.Mvc;
using CLTI.Diagnosis.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                _logger.LogInformation("GetCurrentUser called from {RemoteIp}",
                    HttpContext.Connection.RemoteIpAddress);

                // ✅ ДЕТАЛЬНЕ ЛОГУВАННЯ АВТЕНТИФІКАЦІЇ
                _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuthenticated}",
                    User.Identity?.IsAuthenticated);
                _logger.LogInformation("User.Identity.Name: {Name}",
                    User.Identity?.Name);
                _logger.LogInformation("User.Identity.AuthenticationType: {AuthType}",
                    User.Identity?.AuthenticationType);

                // ✅ ЛОГУВАННЯ ВСІХ CLAIMS
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                }

                // ✅ ЛОГУВАННЯ COOKIES
                foreach (var cookie in Request.Cookies)
                {
                    _logger.LogInformation("Cookie: {Name} = {Value}",
                        cookie.Key, cookie.Value?.Substring(0, Math.Min(20, cookie.Value.Length)) + "...");
                }

                // ✅ ЛОГУВАННЯ HEADERS
                _logger.LogInformation("Authorization Header: {Auth}",
                    Request.Headers.Authorization.ToString());
                _logger.LogInformation("X-User-Id Header: {UserId}",
                    Request.Headers["X-User-Id"].ToString());

                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("User not authenticated - Identity.IsAuthenticated = false");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("UserIdClaim: {UserIdClaim}", userIdClaim);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("NameIdentifier claim is null or empty");

                    // ✅ СПРОБУЄМО АЛЬТЕРНАТИВНІ CLAIM TYPES
                    var altUserId = User.FindFirst("sub")?.Value ??
                                   User.FindFirst("id")?.Value ??
                                   User.FindFirst("userId")?.Value;

                    if (!string.IsNullOrEmpty(altUserId))
                    {
                        _logger.LogInformation("Found alternative user ID: {AltUserId}", altUserId);
                        userIdClaim = altUserId;
                    }
                    else
                    {
                        return Unauthorized(new { error = "User ID claim not found" });
                    }
                }

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Cannot parse user ID: {UserIdClaim}", userIdClaim);
                    return Unauthorized(new { error = "Invalid user ID format" });
                }

                var user = await _userService.GetCurrentUserAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found in database", userId);
                    return NotFound(new { error = "User not found" });
                }

                _logger.LogInformation("Successfully retrieved user: {Email}", user.Email);

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
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        // ✅ ДОДАЄМО ЕНДПОІНТ ДЛЯ ТЕСТУВАННЯ АВТЕНТИФІКАЦІЇ
        [HttpGet("auth-test")]
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
                    Cookies = Request.Cookies.ToDictionary(c => c.Key, c => c.Value?.Length > 20 ? c.Value.Substring(0, 20) + "..." : c.Value),
                    Headers = Request.Headers.Where(h => h.Key.Contains("Auth") || h.Key.Contains("Cookie") || h.Key.Contains("User"))
                                           .ToDictionary(h => h.Key, h => h.Value.ToString()),
                    RequestPath = Request.Path.Value,
                    Method = Request.Method,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Auth test result: {@AuthInfo}", authInfo);
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
                    return Unauthorized(new { error = "User not authenticated" });
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
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var success = await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to change password. Old password might be incorrect." });
                }

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
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to delete user" });
                }

                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

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