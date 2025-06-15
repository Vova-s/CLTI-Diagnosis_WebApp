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

                // ✅ ДЕТАЛЬНЕ ЛОГУВАННЯ
                _logger.LogInformation("Request details: Path={Path}, Method={Method}, " +
                                     "IsAuthenticated={IsAuth}, AuthType={AuthType}, " +
                                     "UserName={UserName}, CookieCount={CookieCount}",
                    HttpContext.Request.Path,
                    HttpContext.Request.Method,
                    User.Identity?.IsAuthenticated ?? false,
                    User.Identity?.AuthenticationType,
                    User.Identity?.Name,
                    HttpContext.Request.Cookies.Count);

                // ✅ ЛОГУВАННЯ COOKIES
                foreach (var cookie in HttpContext.Request.Cookies)
                {
                    if (cookie.Key.Contains("Identity") || cookie.Key.Contains("Auth"))
                    {
                        _logger.LogInformation("Auth Cookie: {Name} = {Value}",
                            cookie.Key,
                            cookie.Value?.Substring(0, Math.Min(20, cookie.Value.Length)) + "...");
                    }
                }

                // ✅ ЛОГУВАННЯ CLAIMS
                if (User.Claims.Any())
                {
                    foreach (var claim in User.Claims)
                    {
                        _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
                    }
                }
                else
                {
                    _logger.LogWarning("No claims found for user");
                }

                // ✅ ПЕРЕВІРКА АВТЕНТИФІКАЦІЇ
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("User not authenticated. Identity: {Identity}, " +
                                     "IsAuthenticated: {IsAuth}, AuthType: {AuthType}",
                        User.Identity,
                        User.Identity?.IsAuthenticated,
                        User.Identity?.AuthenticationType);

                    return Unauthorized(new
                    {
                        error = "User not authenticated",
                        details = new
                        {
                            hasIdentity = User.Identity != null,
                            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                            authenticationType = User.Identity?.AuthenticationType,
                            claimsCount = User.Claims.Count(),
                            cookiesCount = HttpContext.Request.Cookies.Count
                        }
                    });
                }

                // ✅ ОТРИМАННЯ USER ID З CLAIMS
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("NameIdentifier claim: {UserIdClaim}", userIdClaim);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogWarning("NameIdentifier claim is null or empty");

                    // ✅ СПРОБУЄМО АЛЬТЕРНАТИВНІ CLAIM TYPES
                    var altUserId = User.FindFirst("sub")?.Value ??
                                   User.FindFirst("id")?.Value ??
                                   User.FindFirst("userId")?.Value ??
                                   User.FindFirst("user_id")?.Value;

                    if (!string.IsNullOrEmpty(altUserId))
                    {
                        _logger.LogInformation("Found alternative user ID: {AltUserId}", altUserId);
                        userIdClaim = altUserId;
                    }
                    else
                    {
                        _logger.LogError("No user ID claim found. Available claims: {Claims}",
                            string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));

                        return Unauthorized(new
                        {
                            error = "User ID claim not found",
                            availableClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                        });
                    }
                }

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Cannot parse user ID: {UserIdClaim}", userIdClaim);
                    return Unauthorized(new { error = "Invalid user ID format", userIdClaim });
                }

                // ✅ ОТРИМАННЯ КОРИСТУВАЧА З БД
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
                    AuthCookies = HttpContext.Request.Cookies
                        .Where(c => c.Key.Contains("Identity") || c.Key.Contains("Auth") || c.Key.Contains("Cookie"))
                        .ToDictionary(c => c.Key, c => c.Value?.Length > 20 ? c.Value.Substring(0, 20) + "..." : c.Value),
                    AllCookies = HttpContext.Request.Cookies.Count,
                    Headers = HttpContext.Request.Headers
                        .Where(h => h.Key.Contains("Auth") || h.Key.Contains("Cookie") || h.Key.Contains("User"))
                        .ToDictionary(h => h.Key, h => h.Value.ToString()),
                    RequestPath = HttpContext.Request.Path.Value,
                    Method = HttpContext.Request.Method,
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                    RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
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
        [Authorize] // ✅ Додаємо Authorize атрибут
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
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
        [Authorize] // ✅ Додаємо Authorize атрибут
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
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
        [Authorize] // ✅ Додаємо Authorize атрибут
        public async Task<IActionResult> DeleteUser()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                var success = await _userService.DeleteUserAsync(userId);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to delete user" });
                }

                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
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