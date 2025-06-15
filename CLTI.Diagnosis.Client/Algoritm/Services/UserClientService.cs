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

        public UserClientService(HttpClient httpClient, ILogger<UserClientService> logger)
        {
            _httpClient = httpClient;
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
                var response = await _httpClient.GetAsync("/api/user/current");

                if (response.IsSuccessStatusCode)
                {
                    var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>(_jsonOptions);
                    OnUserChanged?.Invoke(userInfo);
                    return userInfo;
                }

                _logger.LogWarning("Failed to get current user: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(string firstName, string lastName, string email)
        {
            try
            {
                var request = new
                {
                    firstName = firstName,
                    lastName = lastName,
                    email = email
                };

                var response = await _httpClient.PutAsJsonAsync("/api/user/update", request, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    // Оновлюємо кешовані дані користувача
                    await GetCurrentUserAsync();
                    return true;
                }

                _logger.LogWarning("Failed to update user: {StatusCode}", response.StatusCode);
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
                var request = new
                {
                    oldPassword = oldPassword,
                    newPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync("/api/user/change-password", request, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password changed successfully");
                    return true;
                }

                _logger.LogWarning("Failed to change password: {StatusCode}", response.StatusCode);
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
                var response = await _httpClient.DeleteAsync("/api/user/delete");

                if (response.IsSuccessStatusCode)
                {
                    OnUserChanged?.Invoke(null);
                    _logger.LogInformation("User deleted successfully");
                    return true;
                }

                _logger.LogWarning("Failed to delete user: {StatusCode}", response.StatusCode);
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