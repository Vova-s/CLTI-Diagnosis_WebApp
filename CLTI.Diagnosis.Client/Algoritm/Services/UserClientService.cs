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
                _logger.LogInformation("Attempting to get current user from API");
                _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress);

                // ✅ ЛОГУВАННЯ ЗАГОЛОВКІВ ЗАПИТУ
                var headers = _httpClient.DefaultRequestHeaders.ToList();
                foreach (var header in headers)
                {
                    if (header.Key.Contains("Cookie") || header.Key.Contains("Auth") || header.Key.Contains("User"))
                    {
                        _logger.LogInformation("Request Header: {Key} = {Value}",
                            header.Key, string.Join("; ", header.Value));
                    }
                }

                var response = await _httpClient.GetAsync("/api/user/current");

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                // ✅ ЛОГУВАННЯ ЗАГОЛОВКІВ ВІДПОВІДІ
                if (response.Headers.Any())
                {
                    foreach (var header in response.Headers)
                    {
                        _logger.LogInformation("Response Header: {Key} = {Value}",
                            header.Key, string.Join("; ", header.Value));
                    }
                }

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
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to get current user: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    // ✅ ЯКЩО 401, СПРОБУЄМО ОТРИМАТИ ДОДАТКОВУ ІНФОРМАЦІЮ
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("User is not authenticated. Attempting auth test...");
                        await TestAuthenticationAsync();
                    }
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

        // ✅ ДОДАЄМО МЕТОД ДЛЯ ТЕСТУВАННЯ АВТЕНТИФІКАЦІЇ
        private async Task TestAuthenticationAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/user/auth-test");
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Auth test result: Status={StatusCode}, Content={Content}",
                    response.StatusCode, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auth test");
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

                _logger.LogInformation("Attempting to update user: {Email}", email);

                var response = await _httpClient.PutAsJsonAsync("/api/user/update", request, _jsonOptions);

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
                var request = new
                {
                    oldPassword = oldPassword,
                    newPassword = newPassword
                };

                _logger.LogInformation("Attempting to change password");

                var response = await _httpClient.PostAsJsonAsync("/api/user/change-password", request, _jsonOptions);

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
                _logger.LogInformation("Attempting to delete user");

                var response = await _httpClient.DeleteAsync("/api/user/delete");

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