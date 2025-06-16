// ✅ Оновлений UserClientService з JWT
using System.Net.Http.Json;
using System.Text.Json;

namespace CLTI.Diagnosis.Client.Services
{
    public interface IUserClientService
    {
        Task<UserInfo?> GetCurrentUserAsync();
        Task<bool> UpdateUserAsync(string firstName, string lastName, string email);
        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
        Task<bool> DeleteUserAsync();
        event Action<UserInfo?>? OnUserChanged;
    }

    public class UserClientService : IUserClientService
    {
        private readonly HttpClient _httpClient;
        private readonly JwtTokenService _tokenService;
        private readonly ILogger<UserClientService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserClientService(
            HttpClient httpClient,
            JwtTokenService tokenService,
            ILogger<UserClientService> logger)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public event Action<UserInfo?>? OnUserChanged;

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to get current user from API with JWT");

                // Отримуємо токен та додаємо до запиту
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available");
                    return null;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/current");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Raw response content: {Content}", responseContent);

                    var userInfo = JsonSerializer.Deserialize<UserInfo>(responseContent, _jsonOptions);

                    if (userInfo != null)
                    {
                        _logger.LogInformation("Successfully retrieved user info for: {Email} (ID: {Id})",
                            userInfo.Email, userInfo.Id);
                        OnUserChanged?.Invoke(userInfo);
                        return userInfo;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize user info from response");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("JWT token is invalid or expired");
                    await _tokenService.RemoveTokenAsync();
                    OnUserChanged?.Invoke(null);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get current user: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while getting current user");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while getting current user");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error while getting current user");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting current user");
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(string firstName, string lastName, string email)
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for update");
                    return false;
                }

                var request = new
                {
                    firstName = firstName,
                    lastName = lastName,
                    email = email
                };

                _logger.LogInformation("Attempting to update user: {Email}", email);

                var httpRequest = new HttpRequestMessage(HttpMethod.Put, "/api/user/update")
                {
                    Content = JsonContent.Create(request, options: _jsonOptions)
                };
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User update successful");
                    // Оновлюємо кешовані дані користувача
                    await GetCurrentUserAsync();
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update user: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for password change");
                    return false;
                }

                var request = new
                {
                    oldPassword = oldPassword,
                    newPassword = newPassword
                };

                _logger.LogInformation("Attempting to change password");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/user/change-password")
                {
                    Content = JsonContent.Create(request, options: _jsonOptions)
                };
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password changed successfully");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to change password: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync()
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for user deletion");
                    return false;
                }

                _logger.LogInformation("Attempting to delete user");

                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    OnUserChanged?.Invoke(null);
                    await _tokenService.RemoveTokenAsync();
                    _logger.LogInformation("User deleted successfully");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to delete user: {StatusCode}, Content: {ErrorContent}",
                    response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return false;
            }
        }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}