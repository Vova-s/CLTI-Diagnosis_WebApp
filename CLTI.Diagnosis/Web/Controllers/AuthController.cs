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
using CLTI.Diagnosis.Core.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

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
            using var transaction = await _context.Database.BeginTransactionAsync();
            
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

                // Find user in database by email first
                var user = await _context.SysUsers
                    .Include(u => u.StatusEnumItem)
                    .Include(u => u.SysUserRoles)
                    .ThenInclude(ur => ur.SysRole)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogWarning("API Login failed for user {Email} - user not found", request.Email);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Verify password with BCrypt or MD5 fallback
                bool isPasswordValid = false;
                bool needsRehash = false;

                if (user.PasswordHashType == "BCrypt" || string.IsNullOrEmpty(user.PasswordHashType))
                {
                    // Try BCrypt first
                    isPasswordValid = VerifyPassword(request.Password, user.Password);
                }
                else if (user.PasswordHashType == "MD5")
                {
                    // Try MD5 for legacy users
                    var md5Hash = HashPasswordMD5(request.Password);
                    isPasswordValid = user.Password == md5Hash;
                    needsRehash = isPasswordValid; // Rehash to BCrypt if MD5 succeeds
                }

                if (!isPasswordValid)
                {
                    _logger.LogWarning("API Login failed for user {Email} - invalid password", request.Email);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Rehash to BCrypt if needed (MD5 user successfully logged in)
                if (needsRehash)
                {
                    user.Password = HashPassword(request.Password);
                    user.PasswordHashType = "BCrypt";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Password rehashed to BCrypt for user {Email}", request.Email);
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

                // Check for recent refresh tokens to prevent duplicates
                var recentToken = await _context.SysRefreshTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && !t.IsRevoked)
                    .Where(t => t.CreatedAt > DateTime.UtcNow.AddSeconds(-5)) // Within last 5 seconds
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                string refreshToken;
                if (recentToken != null)
                {
                    // Use existing recent token to prevent duplicates
                    refreshToken = recentToken.Token;
                    _logger.LogInformation("Using existing refresh token created {SecondsAgo} seconds ago for user {Email}", 
                        (DateTime.UtcNow - recentToken.CreatedAt).TotalSeconds, user.Email);
                }
                else
                {
                    // Generate new refresh token
                    refreshToken = await GenerateRefreshTokenAsync(user.Id);
                }

                await transaction.CommitAsync();
                _logger.LogInformation("API Login successful for user: {Email} (ID: {UserId})", user.Email, user.Id);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new LoginResponse
                    {
                        Token = token,
                        RefreshToken = refreshToken,
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
                await transaction.RollbackAsync();
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

                // Hash password with BCrypt
                var hashedPassword = HashPassword(request.Password);

                // Create new user
                var newUser = new SysUser
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Password = hashedPassword,
                    PasswordHashType = "BCrypt",
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
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token is required"
                    });
                }

                // Find the refresh token in database
                var refreshToken = await _context.SysRefreshTokens
                    .Include(rt => rt.User)
                    .ThenInclude(u => u.StatusEnumItem)
                    .Include(rt => rt.User)
                    .ThenInclude(u => u.SysUserRoles)
                    .ThenInclude(ur => ur.SysRole)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found: {Token}", request.RefreshToken);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    });
                }

                // Check if token is expired
                if (refreshToken.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token expired for user {UserId}", refreshToken.UserId);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token has expired"
                    });
                }

                // Check if token is revoked or already used
                if (refreshToken.IsRevoked || refreshToken.IsUsed)
                {
                    _logger.LogWarning("Refresh token is revoked or used for user {UserId}", refreshToken.UserId);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token has been revoked"
                    });
                }

                // Check if user is still active
                if (refreshToken.User.StatusEnumItem?.Name != "Active")
                {
                    _logger.LogWarning("User {UserId} is not active", refreshToken.UserId);
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User account is not active"
                    });
                }

                // Check for recent refresh tokens to prevent duplicates
                var recentToken = await _context.SysRefreshTokens
                    .Where(t => t.UserId == refreshToken.UserId && !t.IsUsed && !t.IsRevoked)
                    .Where(t => t.CreatedAt > DateTime.UtcNow.AddSeconds(-5)) // Within last 5 seconds
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                string newRefreshToken;
                if (recentToken != null)
                {
                    // Use existing recent token to prevent duplicates
                    newRefreshToken = recentToken.Token;
                    _logger.LogInformation("Using existing refresh token created {SecondsAgo} seconds ago for user {Email}",
                        (DateTime.UtcNow - recentToken.CreatedAt).TotalSeconds, refreshToken.User.Email);
                }
                else
                {
                    // Generate new refresh token
                    newRefreshToken = await GenerateRefreshTokenAsync(refreshToken.UserId);
                }

                // Generate new JWT token
                var newJwtToken = GenerateJwtToken(refreshToken.User, false);

                // Mark old refresh token as used
                refreshToken.IsUsed = true;

                // Link old token to new one for tracking
                refreshToken.ReplacedByToken = newRefreshToken;

                await _context.SaveChangesAsync();

                _logger.LogInformation("API Token refreshed for user: {Email}", refreshToken.User.Email);

                return Ok(new ApiResponse<LoginResponse>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = new LoginResponse
                    {
                        Token = newJwtToken,
                        RefreshToken = newRefreshToken,
                        User = new UserDto
                        {
                            Id = refreshToken.User.Id,
                            FirstName = refreshToken.User.FirstName ?? "",
                            LastName = refreshToken.User.LastName,
                            Email = refreshToken.User.Email,
                            FullName = $"{refreshToken.User.FirstName} {refreshToken.User.LastName}".Trim()
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

        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token is required"
                    });
                }

                // Find the refresh token in database
                var refreshToken = await _context.SysRefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found for revocation: {Token}", request.RefreshToken);
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Refresh token not found"
                    });
                }

                // Revoke the token
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refresh token revoked for user {UserId}", refreshToken.UserId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Refresh token revoked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh token revocation");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during token revocation"
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

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Data = ModelState
                    });
                }

                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Do not reveal whether the user exists
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "If an account with that email exists, a reset link has been sent."
                    });
                }

                // Generate a reset token (HMAC-based, no DB storage required)
                var resetToken = GeneratePasswordResetToken(request.Email);
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?code={Uri.EscapeDataString(resetToken)}";

                _logger.LogInformation("Password reset requested for {Email}. Callback: {Callback}", request.Email, callbackUrl);
                // TODO: Integrate email sending here if/when infrastructure is available

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If an account with that email exists, a reset link has been sent."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during password reset request"
                });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Data = ModelState
                    });
                }

                // Validate token and extract email
                if (!TryValidatePasswordResetToken(request.Code, out var emailFromToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid or expired reset token"
                    });
                }

                if (!string.Equals(emailFromToken, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid reset token for this email"
                    });
                }

                var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // Do not reveal anything
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Your password has been reset if the account exists."
                    });
                }

                // Update password with BCrypt
                user.Password = HashPassword(request.NewPassword);
                user.PasswordHashType = "BCrypt";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successful for {Email}", request.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password reset successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for {Email}", request.Email);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during password reset"
                });
            }
        }

        private string GeneratePasswordResetToken(string email)
        {
            // Token format: base64url(email).base64url(ticks).base64url(hmac)
            var secret = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security";
            var ticks = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = $"{email}|{ticks}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            string B64(byte[] bytes) => Base64UrlEncode(bytes);
            string B64s(string s) => Base64UrlEncode(Encoding.UTF8.GetBytes(s));
            return $"{B64s(email)}.{ticks}.{B64(sig)}";
        }

        private bool TryValidatePasswordResetToken(string token, out string email)
        {
            email = string.Empty;
            try
            {
                var parts = token.Split('.', 3);
                if (parts.Length != 3) return false;
                var emailBytes = Base64UrlDecode(parts[0]);
                email = Encoding.UTF8.GetString(emailBytes);
                if (!long.TryParse(parts[1], out var ticks)) return false;
                var sigProvided = Base64UrlDecode(parts[2]);

                // Check expiry (1 hour)
                var issuedAt = DateTimeOffset.FromUnixTimeSeconds(ticks);
                if (DateTimeOffset.UtcNow - issuedAt > TimeSpan.FromHours(1)) return false;

                var secret = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security";
                var payload = $"{email}|{ticks}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var sigExpected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

                return sigProvided.SequenceEqual(sigExpected);
            }
            catch
            {
                return false;
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }

        #region Private Methods

        private string GenerateJwtToken(SysUser user, bool rememberMe)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CLTI.Diagnosis";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "CLTI.Diagnosis.Client";

            var claims = new List<Claim>
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

            // Add role claims
            foreach (var userRole in user.SysUserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.SysRole.Name));
            }

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
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        private static string HashPasswordMD5(string password)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private async Task<string> GenerateRefreshTokenAsync(int userId)
        {
            // Generate cryptographically secure random token
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            // Create refresh token entity
            var refreshToken = new SysRefreshToken
            {
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiration
                IsRevoked = false,
                IsUsed = false
            };

            _context.SysRefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return token;
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
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class RevokeTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    #endregion
}
