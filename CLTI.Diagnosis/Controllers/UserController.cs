using Microsoft.AspNetCore.Mvc;
using CLTI.Diagnosis.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var user = await _userService.GetCurrentUserAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

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
                return StatusCode(500, new { error = "Internal server error" });
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

                // Вилогінюємо користувача після видалення
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