using System.IdentityModel.Tokens.Jwt;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Infrastructure.Auth
{
    /// <summary>
    /// Сервіс для роботи з JWT токенами
    /// </summary>
    public class JwtTokenService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        // Use shared keys with AuthApiService to keep storage consistent across the app
        private const string TOKEN_KEY = "jwt_token";
        private const string USER_KEY = "current_user";

        public JwtTokenService(IJSRuntime jsRuntime, ILogger<JwtTokenService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Перевіряє чи доступний JavaScript runtime
        /// </summary>
        private bool IsJavaScriptAvailable()
        {
            try
            {
                return _jsRuntime is IJSInProcessRuntime ||
                       !(_jsRuntime.GetType().Name.Contains("UnsupportedJavaScriptRuntime"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Зберігає JWT токен в localStorage
        /// </summary>
        public async Task SetTokenAsync(string token)
        {
            try
            {
                if (!IsJavaScriptAvailable())
                {
                    _logger.LogWarning("JavaScript not available, cannot save token to localStorage");
                    return;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
                _logger.LogInformation("Token saved successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot save token during static rendering: {Error}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving token to localStorage");
                throw;
            }
        }

        /// <summary>
        /// Отримує JWT токен з localStorage
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                _logger.LogDebug("JwtTokenService: GetTokenAsync called, checking JavaScript availability");
                if (!IsJavaScriptAvailable())
                {
                    _logger.LogWarning("JwtTokenService: JavaScript not available, returning null token");
                    return null;
                }

                _logger.LogDebug("JwtTokenService: JavaScript available, accessing localStorage");
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("JwtTokenService: No token found in localStorage");
                    return null;
                }

                // Перевіряємо чи токен ще дійсний
                _logger.LogDebug("JwtTokenService: Token found, validating...");
                if (IsTokenValid(token))
                {
                    _logger.LogDebug("JwtTokenService: Token is valid, returning");
                    return token;
                }
                else
                {
                    _logger.LogWarning("JwtTokenService: Token found but expired, removing from localStorage");
                    await RemoveTokenAsync();
                    return null;
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogDebug("JwtTokenService: Cannot get token during static rendering, returning null");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JwtTokenService: Error getting token from localStorage");
                return null;
            }
        }

        /// <summary>
        /// Видаляє JWT токен з localStorage
        /// </summary>
        public async Task RemoveTokenAsync()
        {
            try
            {
                if (!IsJavaScriptAvailable())
                {
                    _logger.LogWarning("JavaScript not available, cannot remove token from localStorage");
                    return;
                }

                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);
                _logger.LogInformation("Token removed successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot remove token during static rendering: {Error}", ex.Message);
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

        /// <summary>
        /// Зберігає інформацію про користувача
        /// </summary>
        public async Task SetUserAsync(UserInfo user)
        {
            try
            {
                if (!IsJavaScriptAvailable())
                {
                    _logger.LogWarning("JavaScript not available, cannot save user to localStorage");
                    return;
                }

                var userJson = JsonSerializer.Serialize(user);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, userJson);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot save user during static rendering: {Error}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user to localStorage");
                throw;
            }
        }

        /// <summary>
        /// Отримує збережену інформацію про користувача
        /// </summary>
        public async Task<UserInfo?> GetUserAsync()
        {
            try
            {
                if (!IsJavaScriptAvailable())
                {
                    _logger.LogDebug("JavaScript not available, returning null user");
                    return null;
                }

                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
                if (string.IsNullOrEmpty(userJson))
                {
                    _logger.LogWarning("No user data found in localStorage with key: {UserKey}", USER_KEY);
                    return null;
                }

                _logger.LogInformation("Found user data in localStorage: {UserJson}", userJson);

                // Try to deserialize as AuthUserDto first (from AuthApiService)
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var authUserDto = JsonSerializer.Deserialize<AuthUserDto>(userJson, jsonOptions);
                    if (authUserDto != null)
                    {
                        _logger.LogInformation("Successfully deserialized as AuthUserDto: {Email} (ID: {Id})", 
                            authUserDto.Email, authUserDto.Id);
                        return new UserInfo
                        {
                            Id = authUserDto.Id,
                            FirstName = authUserDto.FirstName,
                            LastName = authUserDto.LastName,
                            Email = authUserDto.Email,
                            FullName = authUserDto.FullName
                        };
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning("Failed to deserialize as AuthUserDto: {Error}", ex.Message);
                }

                // Fallback to UserInfo
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson, jsonOptions);
                    if (userInfo != null)
                    {
                        _logger.LogInformation("Successfully deserialized as UserInfo: {Email} (ID: {Id})", 
                            userInfo.Email, userInfo.Id);
                        return userInfo;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize user data as UserInfo: {Error}", ex.Message);
                }

                _logger.LogWarning("Could not deserialize user data from localStorage");
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogDebug("Cannot get user during static rendering, returning null");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user from localStorage");
                return null;
            }
        }
    }
}