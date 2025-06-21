// /app/CLTI.Diagnosis/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Data.Entities;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("API Login attempt for user: {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Data = ModelState
                    });
                }

                // Hash password
                var hashedPassword = HashPassword(request.Password);

                // Find user in database
                var user = await _context.SysUsers
                    .Include(u => u.StatusEnumItem)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == hashedPassword);

                if (user == null)
                {
                    _logger.LogWarning("API Login failed for user {Email} - invalid credentials", request.Email);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check user status
                if (user.StatusEnumItem?.Name != "Active")
                {
                    _logger.LogWarning("API Login failed for user {Email} - account not active", request.Email);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Account is not active"
                    });
                }

                // Generate JWT token
                var token = GenerateJwtToken(user, request.RememberMe);

                _logger.LogInformation("API Login successful for user: {Email} (ID: {UserId})", user.Email, user.Id);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new LoginResponse
                    {
                        Token = token,
                        User = new UserDto
                        {
                            Id = user.Id,
                            FirstName = user.FirstName ?? "",
                            LastName = user.LastName,
                            Email = user.Email,
                            FullName = $"{user.FirstName} {user.LastName}".Trim()
                        },
                        ExpiresAt = DateTime.UtcNow.AddHours(request.RememberMe ? 24 * 30 : 24)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login for user {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("API Registration attempt for user: {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Data = ModelState
                    });
                }

                // Check if user already exists
                var existingUser = await _context.SysUsers
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    _logger.LogWarning("API Registration failed - user {Email} already exists", request.Email);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    });
                }

                // Get or create active status
                var activeStatus = await GetOrCreateActiveStatusAsync();

                // Hash password
                var hashedPassword = HashPassword(request.Password);

                // Create new user
                var newUser = new SysUser
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Password = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    StatusEnumItemId = activeStatus.Id,
                    Guid = Guid.NewGuid()
                };

                _context.SysUsers.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Email} registered successfully with ID {UserId}",
                    newUser.Email, newUser.Id);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Registration successful. You can now log in."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API registration for user {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during registration"
                });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                _logger.LogInformation("API User logout requested");
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API logout");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during logout"
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Token is required"
                    });
                }

                // Decode token without validating signature to get claims
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(request.Token);

                var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid token"
                    });
                }

                // Get user
                var user = await _context.SysUsers
                    .Include(u => u.StatusEnumItem)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || user.StatusEnumItem?.Name != "Active")
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found or not active"
                    });
                }

                // Generate new token
                var newToken = GenerateJwtToken(user, false);

                _logger.LogInformation("API Token refreshed for user: {Email}", user.Email);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = new LoginResponse
                    {
                        Token = newToken,
                        User = new UserDto
                        {
                            Id = user.Id,
                            FirstName = user.FirstName ?? "",
                            LastName = user.LastName,
                            Email = user.Email,
                            FullName = $"{user.FirstName} {user.LastName}".Trim()
                        },
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API token refresh");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during token refresh"
                });
            }
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid user token"
                    });
                }

                var user = await _context.SysUsers
                    .Include(u => u.StatusEnumItem)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = new UserDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName,
                        Email = user.Email,
                        FullName = $"{user.FirstName} {user.LastName}".Trim()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user information"
                });
            }
        }

        #region Private Methods

        private string GenerateJwtToken(SysUser user, bool rememberMe)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CLTI.Diagnosis";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "CLTI.Diagnosis.Client";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim("user_id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("full_name", $"{user.FirstName} {user.LastName}".Trim()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiry = rememberMe
                ? DateTime.UtcNow.AddDays(30)
                : DateTime.UtcNow.AddHours(24);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiry,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task<SysEnumItem> GetOrCreateActiveStatusAsync()
        {
            var activeStatus = await _context.SysEnumItems
                .FirstOrDefaultAsync(e => e.Name == "Active" && e.SysEnum.Name == "UserStatus");

            if (activeStatus == null)
            {
                var statusEnum = await _context.SysEnums
                    .FirstOrDefaultAsync(e => e.Name == "UserStatus");

                if (statusEnum == null)
                {
                    statusEnum = new SysEnum
                    {
                        Name = "UserStatus",
                        OrderingType = "Manual",
                        OrderingTypeEditor = "Manual",
                        Guid = Guid.NewGuid()
                    };
                    _context.SysEnums.Add(statusEnum);
                    await _context.SaveChangesAsync();
                }

                activeStatus = new SysEnumItem
                {
                    SysEnumId = statusEnum.Id,
                    Name = "Active",
                    Value = "1",
                    OrderIndex = 1,
                    Guid = Guid.NewGuid()
                };
                _context.SysEnumItems.Add(activeStatus);
                await _context.SaveChangesAsync();
            }

            return activeStatus;
        }

        #endregion
    }

    #region DTOs

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    #endregion
}