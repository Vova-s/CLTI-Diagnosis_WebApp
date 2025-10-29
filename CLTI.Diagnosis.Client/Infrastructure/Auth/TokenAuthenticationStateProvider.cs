using Microsoft.AspNetCore.Components.Authorization;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace CLTI.Diagnosis.Client.Infrastructure.Auth
{
    /// <summary>
    /// Custom Authentication State Provider для роботи з JWT токенами
    /// </summary>
    public class TokenAuthenticationStateProvider(
        IJSRuntime jsRuntime,
        ILogger<TokenAuthenticationStateProvider> logger,
        HttpClient httpClient)
        : AuthenticationStateProvider
    {
        private const string TOKEN_KEY = "authToken";
        private const string USER_KEY = "currentUser";

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                logger.LogDebug("Getting authentication state...");

                var token = await GetTokenAsync();
                var user = await GetUserAsync();

                if (string.IsNullOrEmpty(token) || user == null)
                {
                    logger.LogDebug("No valid token or user found, returning anonymous state");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Перевіряємо чи токен не застарілий
                if (IsTokenExpired(token))
                {
                    logger.LogWarning("Token is expired, clearing authentication");
                    await ClearAuthenticationAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var claims = CreateClaimsFromUser(user);
                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                logger.LogInformation("User authenticated: {Email} (ID: {UserId})",
                    user.Email, user.Id);

                return new AuthenticationState(principal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting authentication state");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        /// <summary>
        /// Встановлює автентифікацію користувача
        /// </summary>
        public async Task SetAuthenticationAsync(string token, UserInfo user)
        {
            try
            {
                logger.LogInformation("Setting authentication for user: {Email}", user.Email);

                await jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, JsonSerializer.Serialize(user));

                // Додаємо токен до HTTP клієнта
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var claims = CreateClaimsFromUser(user);
                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));

                logger.LogInformation("Authentication set successfully for user: {Email}", user.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting authentication");
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
                logger.LogInformation("Clearing authentication");

                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);

                // Видаляємо токен з HTTP клієнта
                httpClient.DefaultRequestHeaders.Authorization = null;

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));

                logger.LogInformation("Authentication cleared successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing authentication");
                throw;
            }
        }

        /// <summary>
        /// Отримує поточний токен
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting token from localStorage");
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
                var userJson = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
                if (string.IsNullOrEmpty(userJson))
                    return null;

                return JsonSerializer.Deserialize<UserInfo>(userJson);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting user from localStorage");
                return null;
            }
        }

        /// <summary>
        /// Перевіряє чи токен застарілий
        /// </summary>
        private static bool IsTokenExpired(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                    return true;

                var payload = parts[1];
                var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var payloadData = JsonSerializer.Deserialize<JsonElement>(payloadJson);

                if (payloadData.TryGetProperty("exp", out var expElement))
                {
                    var exp = expElement.GetInt64();
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    return expDateTime <= DateTimeOffset.UtcNow;
                }

                return true; // Якщо немає exp claim, вважаємо токен застарілим
            }
            catch
            {
                return true; // При будь-якій помилці вважаємо токен застарілим
            }
        }

        /// <summary>
        /// Створює claims з даних користувача
        /// </summary>
        private static List<Claim> CreateClaimsFromUser(UserInfo user)
        {
            return new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.GivenName, user.FirstName ?? ""),
                new(ClaimTypes.Surname, user.LastName ?? ""),
                new("user_id", user.Id.ToString()),
                new("email", user.Email),
                new("full_name", user.FullName)
            };
        }
    }
}