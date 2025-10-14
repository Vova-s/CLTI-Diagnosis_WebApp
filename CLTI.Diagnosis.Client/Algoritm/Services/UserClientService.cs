// ✅ Оновлений UserClientService з JWT
using System.Net.Http.Headers;
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
        private readonly ILogger<UserClientService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly JwtTokenService _jwtTokenService;

        public UserClientService(
            HttpClient httpClient,
            ILogger<UserClientService> logger,
            JwtTokenService jwtTokenService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            _jwtTokenService = jwtTokenService;
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

                var token = await _jwtTokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available in localStorage");
                    return null;
                }

                _logger.LogInformation("Retrieved JWT token (length: {Length})", token.Length);

                var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/current");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
                var token = await _jwtTokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for update");
                    return false;
                }

                var requestObj = new { firstName, lastName, email };
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/user/update")
                {
                    Content = JsonContent.Create(requestObj, options: _jsonOptions)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User update successful");
                    // Refresh cached user data
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
                var token = await _jwtTokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for password change");
                    return false;
                }

                var requestObj = new { oldPassword, newPassword };
                var response = await _httpClient.PostAsJsonAsync("/api/user/change-password", requestObj, _jsonOptions, cancellationToken: default);

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
                var token = await _jwtTokenService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No JWT token available for delete");
                    return false;
                }
                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    OnUserChanged?.Invoke(null);
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