using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CLTI.Diagnosis.Client.Services
{
    /// <summary>
    /// JWT-based Authentication State Provider для Blazor WebAssembly
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly JwtTokenService _tokenService;
        private readonly ILogger<JwtAuthenticationStateProvider> _logger;

        public JwtAuthenticationStateProvider(
            JwtTokenService tokenService,
            ILogger<JwtAuthenticationStateProvider> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                _logger.LogDebug("Getting authentication state...");

                var token = await _tokenService.GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("No token found, returning anonymous state");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Валідуємо токен та отримуємо claims
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
                await _tokenService.SetTokenAsync(token);
                await _tokenService.SetUserAsync(user);

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

                await _tokenService.RemoveTokenAsync();

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
        /// Перевіряє чи користувач автентифікований
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var claims = _tokenService.GetClaimsFromToken(token);
            return claims != null && claims.Any();
        }

        /// <summary>
        /// Отримує поточного користувача
        /// </summary>
        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                // Спочатку намагаємося отримати з токена
                var userFromToken = _tokenService.GetUserFromToken(token);
                if (userFromToken != null)
                    return userFromToken;

                // Якщо не вдалося з токена, отримуємо збережену інформацію
                return await _tokenService.GetUserAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        /// <summary>
        /// Оновлює стан автентифікації
        /// </summary>
        public void NotifyAuthenticationStateChangedManually()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}