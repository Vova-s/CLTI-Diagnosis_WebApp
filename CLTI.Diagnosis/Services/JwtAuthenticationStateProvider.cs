using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace CLTI.Diagnosis.Services
{
    /// <summary>
    /// JWT-based Authentication State Provider
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<JwtAuthenticationStateProvider> _logger;
        private readonly JwtTokenService _tokenService;

        private const string TOKEN_KEY = "jwt_token";
        private const string USER_KEY = "current_user";

        public JwtAuthenticationStateProvider(
            IJSRuntime jsRuntime,
            ILogger<JwtAuthenticationStateProvider> logger,
            JwtTokenService tokenService)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
            _tokenService = tokenService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                _logger.LogDebug("Getting authentication state...");

                var token = await GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("No token found, returning anonymous state");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Валідуємо токен
                var claims = _tokenService.GetClaimsFromToken(token);
                if (claims == null || !claims.Any())
                {
                    _logger.LogWarning("Token is invalid or expired, clearing authentication");
                    await ClearAuthenticationAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                _logger.LogInformation("User authenticated: {Email} (ID: {UserId})", email, userId);

                return new AuthenticationState(principal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        /// <summary>
        /// Встановлює автентифікацію користувача з JWT токеном
        /// </summary>
        public async Task SetAuthenticationAsync(string token, UserInfo user)
        {
            try
            {
                _logger.LogInformation("Setting authentication for user: {Email}", user.Email);

                // Зберігаємо токен та інформацію про користувача
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, JsonSerializer.Serialize(user));

                // Отримуємо claims з токена
                var claims = _tokenService.GetClaimsFromToken(token);
                if (claims != null && claims.Any())
                {
                    var identity = new ClaimsIdentity(claims, "jwt");
                    var principal = new ClaimsPrincipal(identity);

                    NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
                    _logger.LogInformation("Authentication set successfully for user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogError("Invalid token provided for user: {Email}", user.Email);
                    throw new InvalidOperationException("Invalid JWT token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting authentication");
                throw;
            }
        }

        /// <summary>
        /// Очищає автентифікацію
        /// </summary>
        public async Task ClearAuthenticationAsync()
        {
            try
            {
                _logger.LogInformation("Clearing authentication");

                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));

                _logger.LogInformation("Authentication cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing authentication");
                throw;
            }
        }

        /// <summary>
        /// Отримує поточний JWT токен
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token from localStorage");
                return null;
            }
        }

        /// <summary>
        /// Отримує поточного користувача
        /// </summary>
        public async Task<UserInfo?> GetUserAsync()
        {
            try
            {
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
                if (string.IsNullOrEmpty(userJson))
                    return null;

                return JsonSerializer.Deserialize<UserInfo>(userJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user from localStorage");
                return null;
            }
        }

        /// <summary>
        /// Перевіряє чи користувач автентифікований
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var claims = _tokenService.GetClaimsFromToken(token);
            return claims != null && claims.Any();
        }
    }

    /// <summary>
    /// Модель інформації про користувача
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}