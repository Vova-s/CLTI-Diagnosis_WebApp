// /app/CLTI.Diagnosis.Client/Services/AuthApiService.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string TOKEN_KEY = "jwt_token";
        private const string USER_KEY = "current_user";

        public AuthApiService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthApiResult<AuthLoginResponse>> LoginAsync(string email, string password, bool rememberMe = false)
        {
            try
            {
                var request = new
                {
                    email = email,
                    password = password,
                    rememberMe = rememberMe
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<AuthLoginResponse>>(content, _jsonOptions);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // Store token and user info
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, apiResponse.Data.Token);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, JsonSerializer.Serialize(apiResponse.Data.User, _jsonOptions));

                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

                        return new AuthApiResult<AuthLoginResponse>
                        {
                            Success = true,
                            Data = apiResponse.Data,
                            Message = apiResponse.Message
                        };
                    }
                }

                var errorResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = errorResponse?.Message ?? "Login failed"
                };
            }
            catch (Exception ex)
            {
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<object>> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                var request = new
                {
                    email = email,
                    password = password,
                    firstName = firstName,
                    lastName = lastName
                };

                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
                var content = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);

                return new AuthApiResult<object>
                {
                    Success = apiResponse?.Success ?? false,
                    Message = apiResponse?.Message ?? "Registration failed",
                    Data = apiResponse?.Data
                };
            }
            catch (Exception ex)
            {
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<object>> LogoutAsync()
        {
            try
            {
                // Call logout endpoint
                await _httpClient.PostAsync("/api/auth/logout", null);

                // Clear local storage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);

                // Clear authorization header
                _httpClient.DefaultRequestHeaders.Authorization = null;

                return new AuthApiResult<object>
                {
                    Success = true,
                    Message = "Logged out successfully"
                };
            }
            catch (Exception ex)
            {
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = $"Logout error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<AuthUserDto>> GetCurrentUserAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    return new AuthApiResult<AuthUserDto>
                    {
                        Success = false,
                        Message = "No authentication token"
                    };
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("/api/auth/me");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<AuthUserDto>>(content, _jsonOptions);

                    return new AuthApiResult<AuthUserDto>
                    {
                        Success = apiResponse?.Success ?? false,
                        Data = apiResponse?.Data,
                        Message = apiResponse?.Message ?? ""
                    };
                }

                var errorResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);
                return new AuthApiResult<AuthUserDto>
                {
                    Success = false,
                    Message = errorResponse?.Message ?? "Failed to get user info"
                };
            }
            catch (Exception ex)
            {
                return new AuthApiResult<AuthUserDto>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AuthUserDto?> GetStoredUserAsync()
        {
            try
            {
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", USER_KEY);
                if (string.IsNullOrEmpty(userJson))
                    return null;

                return JsonSerializer.Deserialize<AuthUserDto>(userJson, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            var user = await GetStoredUserAsync();
            return !string.IsNullOrEmpty(token) && user != null;
        }
    }

    #region DTOs
    public class AuthApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AuthApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AuthLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public AuthUserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class AuthUserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
    #endregion
}