using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace CLTI.Diagnosis.Client.Services
{
    /// <summary>
    /// Сервіс для автентифікації на основі токенів
    /// </summary>
    public class TokenBasedAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenAuthenticationStateProvider _authStateProvider;
        private readonly ILogger<TokenBasedAuthService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public TokenBasedAuthService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider,
            ILogger<TokenBasedAuthService> logger)
        {
            _httpClient = httpClient;
            _authStateProvider = (TokenAuthenticationStateProvider)authStateProvider;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Виконує вхід користувача
        /// </summary>
        public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
        {
            try
            {
                _logger.LogInformation("Attempting login for user: {Email}", email);

                var loginRequest = new
                {
                    email = email,
                    password = password,
                    rememberMe = rememberMe
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, _jsonOptions);

                    if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                    {
                        _logger.LogInformation("Login successful for user: {Email}", email);

                        // Встановлюємо автентифікацію
                        await _authStateProvider.SetAuthenticationAsync(loginResponse.Token, loginResponse.User);

                        return new AuthResult { Success = true, Message = "Login successful" };
                    }
                    else
                    {
                        _logger.LogWarning("Invalid login response format");
                        return new AuthResult { Success = false, Message = "Invalid server response" };
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Login failed: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                        return new AuthResult
                        {
                            Success = false,
                            Message = errorResponse?.Message ?? "Login failed"
                        };
                    }
                    catch
                    {
                        return new AuthResult { Success = false, Message = "Login failed" };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResult { Success = false, Message = "An error occurred during login" };
            }
        }

        /// <summary>
        /// Виконує реєстрацію користувача
        /// </summary>
        public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                _logger.LogInformation("Attempting registration for user: {Email}", email);

                var registerRequest = new
                {
                    email = email,
                    password = password,
                    firstName = firstName,
                    lastName = lastName
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerRequest, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var registerResponse = JsonSerializer.Deserialize<RegisterResponse>(responseContent, _jsonOptions);

                    _logger.LogInformation("Registration successful for user: {Email}", email);

                    return new AuthResult
                    {
                        Success = true,
                        Message = registerResponse?.Message ?? "Registration successful"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Registration failed: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                        return new AuthResult
                        {
                            Success = false,
                            Message = errorResponse?.Message ?? "Registration failed"
                        };
                    }
                    catch
                    {
                        return new AuthResult { Success = false, Message = "Registration failed" };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new AuthResult { Success = false, Message = "An error occurred during registration" };
            }
        }

        /// <summary>
        /// Виконує вихід користувача
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("Logging out user");

                // Спробуємо повідомити сервер про logout (опціонально)
                try
                {
                    await _httpClient.PostAsync("/api/auth/logout", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify server about logout");
                }

                // Очищаємо локальну автентифікацію
                await _authStateProvider.ClearAuthenticationAsync();

                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        /// <summary>
        /// Отримує поточного користувача
        /// </summary>
        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                var token = await _authStateProvider.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("No token available, user not authenticated");
                    return null;
                }

                // Спочатку спробуємо отримати з localStorage
                var cachedUser = await _authStateProvider.GetUserAsync();
                if (cachedUser != null)
                {
                    _logger.LogDebug("Retrieved user from cache: {Email}", cachedUser.Email);
                    return cachedUser;
                }

                // Якщо немає в кеші, запитаємо сервер
                _logger.LogDebug("Fetching current user from server");

                var response = await _httpClient.GetAsync("/api/user/current");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, _jsonOptions);

                    if (userInfo != null)
                    {
                        _logger.LogInformation("Successfully retrieved user from server: {Email}", userInfo.Email);
                        return userInfo;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("User is not authenticated, clearing local authentication");
                    await _authStateProvider.ClearAuthenticationAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get current user: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        /// <summary>
        /// Перевіряє чи користувач автентифікований
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }
    }

    #region DTO Classes

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserInfo User { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    #endregion
}