using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Services
{
    /// <summary>
    /// Сервіс для роботи з JWT токенами
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        private const string TOKEN_KEY = "jwt_token";

        public JwtTokenService(
            IConfiguration configuration,
            IJSRuntime jsRuntime,
            ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _jsRuntime = jsRuntime;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Отримує JWT токен з localStorage
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("No token found in localStorage");
                    return null;
                }

                // Перевіряємо чи токен ще дійсний
                if (IsTokenValid(token))
                {
                    return token;
                }
                else
                {
                    _logger.LogWarning("Token found but expired, removing from localStorage");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token from localStorage");
                return null;
            }
        }

        /// <summary>
        /// Зберігає JWT токен в localStorage
        /// </summary>
        public async Task SetTokenAsync(string token)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
                _logger.LogDebug("Token stored in localStorage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing token in localStorage");
                throw;
            }
        }

        /// <summary>
        /// Видаляє JWT токен з localStorage
        /// </summary>
        public async Task RemoveTokenAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                _logger.LogDebug("Token removed from localStorage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing token from localStorage");
                throw;
            }
        }

        /// <summary>
        /// Перевіряє чи токен ще дійсний
        /// </summary>
        public bool IsTokenValid(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                if (!_tokenHandler.CanReadToken(token))
                    return false;

                var jwtToken = _tokenHandler.ReadJwtToken(token);

                // Перевіряємо термін дії
                if (jwtToken.ValidTo <= DateTime.UtcNow)
                {
                    _logger.LogDebug("Token expired at {ExpiredAt}", jwtToken.ValidTo);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        /// <summary>
        /// Отримує claims з JWT токена
        /// </summary>
        public IEnumerable<Claim>? GetClaimsFromToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || !_tokenHandler.CanReadToken(token))
                    return null;

                var jwtToken = _tokenHandler.ReadJwtToken(token);

                // Перевіряємо термін дії
                if (jwtToken.ValidTo <= DateTime.UtcNow)
                {
                    _logger.LogDebug("Cannot get claims - token expired");
                    return null;
                }

                return jwtToken.Claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting claims from token");
                return null;
            }
        }

        /// <summary>
        /// Отримує інформацію про користувача з JWT токена
        /// </summary>
        public UserInfo? GetUserFromToken(string token)
        {
            try
            {
                var claims = GetClaimsFromToken(token);
                if (claims == null)
                    return null;

                var claimsList = claims.ToList();

                var userIdClaim = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return null;

                return new UserInfo
                {
                    Id = userId,
                    Email = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "",
                    FirstName = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "",
                    LastName = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "",
                    FullName = claimsList.FirstOrDefault(c => c.Type == "full_name")?.Value ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user from token");
                return null;
            }
        }

        /// <summary>
        /// Валідує JWT токен за допомогою ключа
        /// </summary>
        public bool ValidateToken(string token)
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-min-256-bits-long-for-security-purposes-12345";
                var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CLTI.Diagnosis";
                var jwtAudience = _configuration["Jwt:Audience"] ?? "CLTI.Diagnosis.Client";

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                _tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                return validatedToken != null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Token validation failed: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Отримує час закінчення дії токена
        /// </summary>
        public DateTime? GetTokenExpiration(string token)
        {
            try
            {
                if (!_tokenHandler.CanReadToken(token))
                    return null;

                var jwtToken = _tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token expiration");
                return null;
            }
        }

        /// <summary>
        /// Перевіряє чи токен скоро закінчиться (менше ніж за 5 хвилин)
        /// </summary>
        public bool IsTokenExpiringSoon(string token)
        {
            var expiration = GetTokenExpiration(token);
            if (!expiration.HasValue)
                return true;

            return expiration.Value <= DateTime.UtcNow.AddMinutes(5);
        }
    }
}