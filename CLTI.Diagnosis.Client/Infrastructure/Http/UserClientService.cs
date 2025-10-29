// ✅ Оновлений UserClientService з JWT
using CLTI.Diagnosis.Client.Infrastructure.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CLTI.Diagnosis.Client.Infrastructure.Http
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
        private readonly AuthApiService _authApiService;

        public UserClientService(
            HttpClient httpClient,
            ILogger<UserClientService> logger,
            AuthApiService authApiService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
            _authApiService = authApiService;
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
                _logger.LogInformation("Attempting to get current user from server-side session");

                // ✅ Get user via API - token is automatically added by SessionTokenMiddleware
                var result = await _authApiService.GetCurrentUserAsync();
                
                if (result.Success && result.Data != null)
                {
                    var authUserDto = result.Data;
                    var userInfo = new UserInfo
                    {
                        Id = authUserDto.Id,
                        FirstName = authUserDto.FirstName,
                        LastName = authUserDto.LastName,
                        Email = authUserDto.Email,
                        FullName = authUserDto.FullName
                    };

                    _logger.LogInformation("✅ Successfully retrieved user from server-side session: {Email} (ID: {Id})",
                        userInfo.Email, userInfo.Id);
                    
                    OnUserChanged?.Invoke(userInfo);
                    return userInfo;
                }
                else
                {
                    _logger.LogWarning("❌ Failed to get user from server-side session: {Message}", result.Message);
                    OnUserChanged?.Invoke(null);
                    return null;
                }
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
                // ✅ Token is automatically added by SessionTokenMiddleware
                var requestObj = new { firstName, lastName, email };
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/user/update")
                {
                    Content = JsonContent.Create(requestObj, options: _jsonOptions)
                };
                // No need to manually set Authorization - SessionTokenMiddleware does it

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
                // ✅ Token is automatically added by SessionTokenMiddleware
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
                // ✅ Token is automatically added by SessionTokenMiddleware
                var request = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete");
                // No need to manually set Authorization - SessionTokenMiddleware does it
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