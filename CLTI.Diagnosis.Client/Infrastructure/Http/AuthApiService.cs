// /app/CLTI.Diagnosis.Client/Services/AuthApiService.cs
using CLTI.Diagnosis.Client.Infrastructure.Auth;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Infrastructure.Http
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<AuthApiService> _logger;
        private readonly string _baseUrl;

        // ✅ Tokens are stored server-side in session, not in localStorage
        // These constants are kept for backward compatibility but not used

        public AuthApiService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthApiService> logger)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;

            // Use HttpClient's BaseAddress from configuration
            _baseUrl = _httpClient.BaseAddress?.ToString() ?? "https://localhost:7124/";
            
            // Ensure URL ends with slash
            if (!_baseUrl.EndsWith("/"))
                _baseUrl += "/";

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
                        _logger.LogInformation("🎉 Login successful | User ID: {UserId}", apiResponse.Data.User?.Id);

                        // ✅ Try to set userId cookie via JavaScript as fallback
                        // Browsers often block Set-Cookie headers from Fetch API responses (cross-origin restrictions)
                        // JavaScript fallback ensures cookie is set even if header is blocked
                        if (apiResponse.Data.User?.Id != null)
                        {
                            try
                            {
                                var userId = apiResponse.Data.User.Id;
                                var expiresDate = DateTimeOffset.UtcNow.AddDays(30).ToUniversalTime();
                                
                                // Note: Cannot set HttpOnly via JavaScript, so we set it without HttpOnly
                                // This is acceptable since userId is not sensitive (user ID is already known)
                                var cookieValue = $"_userId={userId}; Path=/; Expires={expiresDate:r}; SameSite=Lax; Secure";
                                
                                // Set cookie via JavaScript
                                await _jsRuntime.InvokeVoidAsync("eval", 
                                    $@"(function() {{ document.cookie = '{cookieValue}'; }})();");
                                
                                _logger.LogInformation("✅ Set _userId cookie via JavaScript fallback: {UserId}", userId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to set cookie via JavaScript fallback: {Error}", ex.Message);
                            }
                        }

                        // ✅ Tokens are stored server-side in session (not in localStorage)
                        // The SessionTokenMiddleware will automatically add the token to API requests
                        // No need to store tokens client-side or set Authorization header manually
                        
                        _logger.LogInformation("✅ Authentication tokens stored in server-side session");

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
                // Call logout endpoint - this will clear server-side session
                try
                {
                    var logoutUrl = $"{_baseUrl}api/auth/logout";
                    await _httpClient.PostAsync(logoutUrl, null);
                    _logger.LogInformation("✅ Logout successful - server-side session cleared");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error calling logout endpoint: {Error}", ex.Message);
                }

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
                // ✅ Token is automatically added by SessionTokenMiddleware
                // No need to manually set Authorization header
                var userUrl = $"{_baseUrl}api/auth/me";
                var response = await _httpClient.GetAsync(userUrl);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("GetCurrentUserAsync response: Status={StatusCode}, ContentLength={Length}", 
                    response.StatusCode, content?.Length ?? 0);

                if (response.IsSuccessStatusCode)
                {
                    // Check if content is empty or not valid JSON
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("GetCurrentUserAsync: Empty response received");
                        return new AuthApiResult<AuthUserDto>
                        {
                            Success = false,
                            Message = "Empty response from server"
                        };
                    }

                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<AuthUserDto>>(content, _jsonOptions);

                        return new AuthApiResult<AuthUserDto>
                        {
                            Success = apiResponse?.Success ?? false,
                            Data = apiResponse?.Data,
                            Message = apiResponse?.Message ?? ""
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "GetCurrentUserAsync: Failed to deserialize response | Content: {Content}", 
                            content != null && content.Length > 200 ? content.Substring(0, 200) : content);
                        return new AuthApiResult<AuthUserDto>
                        {
                            Success = false,
                            Message = "Invalid response format from server"
                        };
                    }
                }

                // Handle non-success status codes
                _logger.LogWarning("GetCurrentUserAsync: Request failed | Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, content != null && content.Length > 200 ? content.Substring(0, 200) : content);

                // Try to parse error response if content is not empty
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);
                        return new AuthApiResult<AuthUserDto>
                        {
                            Success = false,
                            Message = errorResponse?.Message ?? $"Server returned {response.StatusCode}"
                        };
                    }
                    catch (JsonException)
                    {
                        // If we can't parse the error, just return the status code message
                    }
                }

                return new AuthApiResult<AuthUserDto>
                {
                    Success = false,
                    Message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                        ? "Not authenticated" 
                        : $"Server returned {response.StatusCode}"
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

        // ✅ Tokens are stored server-side in session, not in localStorage
        // These methods are kept for backward compatibility but do nothing
        private async Task StoreTokenAsync(string token)
        {
            _logger.LogDebug("StoreTokenAsync called - tokens are stored server-side, no localStorage needed");
            await Task.CompletedTask;
        }

        private async Task StoreRefreshTokenAsync(string refreshToken)
        {
            _logger.LogDebug("StoreRefreshTokenAsync called - tokens are stored server-side, no localStorage needed");
            await Task.CompletedTask;
        }

        private async Task StoreUserAsync(AuthUserDto user)
        {
            _logger.LogDebug("StoreUserAsync called - user data is stored server-side, no localStorage needed");
            await Task.CompletedTask;
        }

        public async Task<string?> GetTokenAsync()
        {
            // ✅ Tokens are stored server-side in session, not in localStorage
            // This method is kept for backward compatibility but always returns null
            // The SessionTokenMiddleware automatically adds the token to requests
            _logger.LogDebug("GetTokenAsync called - tokens are stored server-side");
            return null;
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            // ✅ Tokens are stored server-side in session, not in localStorage
            // This method is kept for backward compatibility but always returns null
            _logger.LogDebug("GetRefreshTokenAsync called - tokens are stored server-side");
            return null;
        }

        public async Task<AuthUserDto?> GetStoredUserAsync()
        {
            // ✅ User data is stored server-side in session
            // Get user via API call instead of localStorage
            var result = await GetCurrentUserAsync();
            return result.Success ? result.Data : null;
        }

        // ✅ Tokens are stored server-side in session
        // Logout endpoint clears the session, no localStorage needed
        private async Task RemoveTokenAsync()
        {
            _logger.LogDebug("RemoveTokenAsync called - tokens are stored server-side, no localStorage to clear");
            await Task.CompletedTask;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // ✅ Check authentication via API call to /api/auth/me
            // This uses server-side session, not localStorage
            var user = await GetStoredUserAsync();
            return user != null;
        }

        // ✅ Tokens are stored server-side in session
        // These methods are kept for backward compatibility but do nothing
        public async Task StorePendingTokensAsync()
        {
            _logger.LogDebug("StorePendingTokensAsync called - tokens are stored server-side, no action needed");
            await Task.CompletedTask;
        }

        public async Task TryFlushPendingAsync()
        {
            _logger.LogDebug("TryFlushPendingAsync called - tokens are stored server-side, no action needed");
            await Task.CompletedTask;
        }

        public async Task<AuthApiResult<object>> ForgotPasswordAsync(string inputEmail)
        {
            try
            {
                var request = new { email = inputEmail };
                var forgotPasswordUrl = $"{_baseUrl}api/auth/forgot-password";
                
                var response = await _httpClient.PostAsJsonAsync(forgotPasswordUrl, request, _jsonOptions);
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);

                return new AuthApiResult<object>
                {
                    Success = apiResponse?.Success ?? false,
                    Message = apiResponse?.Message ?? "Password reset request failed",
                    Data = apiResponse?.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password request");
                return new AuthApiResult<object>
                {
                    Success = false,
                    Message = $"Network error: {ex.Message}"
                };
            }
        }

        public async Task<AuthApiResult<AuthLoginResponse>> RefreshTokenAsync()
        {
            try
            {
                // ✅ Refresh token is stored server-side in session
                // Server will retrieve it automatically from session, no need to send it
                var refreshUrl = $"{_baseUrl}api/auth/refresh";
                
                // Send empty request - server will get refresh token from session
                var response = await _httpClient.PostAsJsonAsync(refreshUrl, new { }, _jsonOptions);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<AuthApiResponse<AuthLoginResponse>>(content, _jsonOptions);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        // ✅ New tokens are stored server-side automatically by the server
                        _logger.LogInformation("✅ Token refreshed - new tokens stored in server-side session");

                        return new AuthApiResult<AuthLoginResponse>
                        {
                            Success = true,
                            Data = apiResponse.Data,
                            Message = "Token refreshed successfully"
                        };
                    }
                }

                var errorResponse = JsonSerializer.Deserialize<AuthApiResponse<object>>(content, _jsonOptions);
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = errorResponse?.Message ?? "Token refresh failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new AuthApiResult<AuthLoginResponse>
                {
                    Success = false,
                    Message = $"Token refresh error: {ex.Message}"
                };
            }
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
        public string Token { get; set; } = string.Empty; // Empty - stored server-side
        public string RefreshToken { get; set; } = string.Empty; // Empty - stored server-side
        public AuthUserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
        public string? SessionId { get; set; } // For client to use if cookies blocked
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