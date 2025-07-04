﻿// /app/CLTI.Diagnosis.Client/Services/AuthApiService.cs
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
        private readonly ILogger<AuthApiService> _logger;
        private readonly string _baseUrl;

        private const string TOKEN_KEY = "jwt_token";
        private const string USER_KEY = "current_user";

        public AuthApiService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthApiService> logger)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;

            // Визначаємо базову URL напряму в сервісі
            #if DEBUG
                _baseUrl = "https://localhost:7124/";
            #else
                _baseUrl = "https://antsdemo02.demo.dragon-cloud.org/";
            #endif

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInformation("AuthApiService initialized with base URL: {BaseUrl}", _baseUrl);
        }

        public async Task<AuthApiResult<AuthLoginResponse>> LoginAsync(string email, string password, bool rememberMe = false)
        {
            try
            {
                _logger.LogInformation("Attempting login for user: {Email}", email);

                var request = new
                {
                    email = email,
                    password = password,
                    rememberMe = rememberMe
                };

                var loginUrl = $"{_baseUrl}api/auth/login";
                _logger.LogInformation("Sending login request to: {LoginUrl}", loginUrl);

                var response = await _httpClient.PostAsJsonAsync(loginUrl, request, _jsonOptions);

                _logger.LogInformation("Login response status: {StatusCode}", response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Login response content: {Content}", content);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<AuthLoginResponse>>(content, _jsonOptions);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _logger.LogInformation("Login successful, storing token and user info");

                        // Store token and user info safely
                        await StoreTokenAsync(apiResponse.Data.Token);
                        await StoreUserAsync(apiResponse.Data.User);

                        // Set authorization header for future requests
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiResponse.Data.Token);

                        return new AuthApiResult<AuthLoginResponse>
                        {
                            Success = true,
                            Data = apiResponse.Data,
                            Message = apiResponse.Message ?? "Login successful"
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
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during login");
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout during login");
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = "Request timed out"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<object>> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            try
            {
                _logger.LogInformation("Attempting registration for user: {Email}", email);

                var request = new
                {
                    email = email,
                    password = password,
                    firstName = firstName,
                    lastName = lastName
                };

                var registerUrl = $"{_baseUrl}api/auth/register";
                _logger.LogInformation("Sending registration request to: {RegisterUrl}", registerUrl);

                var response = await _httpClient.PostAsJsonAsync(registerUrl, request, _jsonOptions);

                _logger.LogInformation("Registration response status: {StatusCode}", response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Registration response content: {Content}", content);

                var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);

                return new AuthApiResult<object>
                {
                    Success = apiResponse?.Success ?? false,
                    Message = apiResponse?.Message ?? "Registration failed",
                    Data = apiResponse?.Data
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during registration");
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout during registration");
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = "Request timed out"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<object>> LogoutAsync()
        {
            try
            {
                // Call logout endpoint if available
                try
                {
                    var logoutUrl = $"{_baseUrl}api/auth/logout";
                    await _httpClient.PostAsync(logoutUrl, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calling logout endpoint, continuing with local cleanup");
                }

                // Clear local storage
                await RemoveTokenAsync();

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
                _logger.LogError(ex, "Error during logout");
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

                var userUrl = $"{_baseUrl}api/auth/me";
                var response = await _httpClient.GetAsync(userUrl);
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
                _logger.LogError(ex, "Error getting current user");
                return new AuthApiResult<AuthUserDto>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        private async Task StoreTokenAsync(string token)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot store token during static rendering");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing token");
            }
        }

        private async Task StoreUserAsync(AuthUserDto user)
        {
            try
            {
                var userJson = JsonSerializer.Serialize(user, _jsonOptions);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", USER_KEY, userJson);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot store user during static rendering");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing user");
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TOKEN_KEY);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogDebug("Cannot get token during static rendering");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token");
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogDebug("Cannot get user during static rendering");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stored user");
                return null;
            }
        }

        private async Task RemoveTokenAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", USER_KEY);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered"))
            {
                _logger.LogWarning("Cannot remove token during static rendering");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing token");
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