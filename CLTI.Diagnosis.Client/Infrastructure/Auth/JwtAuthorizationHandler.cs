using System.Net.Http.Headers;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Infrastructure.Auth
{
    /// <summary>
    /// JWT Authorization Handler that automatically attaches JWT tokens to HTTP requests
    /// and handles token refresh on 401 responses
    /// </summary>
    public class JwtAuthorizationHandler : DelegatingHandler
    {
    private readonly JwtTokenService _tokenService;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<JwtAuthorizationHandler> _logger;
    private readonly NavigationManager _navigation;

        // Prevent concurrent refresh operations
        private static readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

        public JwtAuthorizationHandler(
            JwtTokenService tokenService,
            HttpClient httpClient,
            IJSRuntime jsRuntime,
            ILogger<JwtAuthorizationHandler> logger,
            NavigationManager navigation)
        {
            Console.WriteLine("JwtAuthorizationHandler: Constructor called - handler is being instantiated");
            _tokenService = tokenService;
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = logger;
            _navigation = navigation;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"JwtAuthorizationHandler: Processing request to: {request.RequestUri}");
                _logger.LogInformation("JwtAuthorizationHandler: Processing request to: {Uri}", request.RequestUri);

                // Skip adding authorization header for auth endpoints (login, register, refresh)
                if (IsAuthEndpoint(request.RequestUri))
                {
                    _logger.LogDebug("Skipping authorization for auth endpoint: {Uri}", request.RequestUri);
                    return await base.SendAsync(request, cancellationToken);
                }

                // Get JWT token and add to request
                Console.WriteLine($"JwtAuthorizationHandler: Getting token for request to {request.RequestUri}");
                _logger.LogDebug("JwtAuthorizationHandler: Getting token for request to {Uri}", request.RequestUri);

                try
                {
                    var token = await _tokenService.GetTokenAsync();

                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        Console.WriteLine($"JwtAuthorizationHandler: Added JWT token to request: {request.RequestUri}");
                        _logger.LogInformation("JwtAuthorizationHandler: Added JWT token to request: {Uri}", request.RequestUri);
                        _logger.LogDebug("JwtAuthorizationHandler: Token length: {TokenLength}", token.Length);
                    }
                    else
                    {
                        Console.WriteLine($"JwtAuthorizationHandler: No JWT token available for request: {request.RequestUri}");
                        _logger.LogWarning("JwtAuthorizationHandler: No JWT token available for request: {Uri}", request.RequestUri);

                        // Try to get user info to see if authentication state is available
                        try
                        {
                            var user = await _tokenService.GetUserAsync();
                            _logger.LogDebug("JwtAuthorizationHandler: User info available: {HasUser}", user != null);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("JwtAuthorizationHandler: Error getting user info: {Error}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JwtAuthorizationHandler: Error getting token: {ex.Message}");
                    _logger.LogError("JwtAuthorizationHandler: Error getting token: {Error}", ex.Message);
                }

                // Send the request
                var response = await base.SendAsync(request, cancellationToken);

                // Handle 401 Unauthorized responses
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Received 401 Unauthorized for request: {Uri}", request.RequestUri);

                    // Attempt to refresh the token
                    if (await TryRefreshTokenWithHttpClient())
                    {
                        // Retry the request once with the new token
                        _logger.LogInformation("Retrying request after token refresh: {Uri}", request.RequestUri);

                        var retryRequest = await CloneHttpRequestMessageAsync(request);
                        var newToken = await _tokenService.GetTokenAsync();

                        if (!string.IsNullOrEmpty(newToken))
                        {
                            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                            response = await base.SendAsync(retryRequest, cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInformation("Request succeeded after token refresh: {Uri}", request.RequestUri);
                                return response;
                            }
                        }
                    }

                    // If refresh failed or retry didn't work, clear authentication
                    _logger.LogWarning("Token refresh failed or retry unsuccessful, clearing authentication");
                    await _tokenService.RemoveTokenAsync();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JwtAuthorizationHandler for request to {Uri}", request.RequestUri);
                throw;
            }
        }

        /// <summary>
        /// Attempts to refresh the JWT token using the refresh token
        /// </summary>
        private async Task<bool> TryRefreshTokenWithHttpClient()
        {
            try
            {
                _logger.LogInformation("Attempting to refresh JWT token with HttpClient");

                // Get refresh token directly from localStorage
                var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refresh_token");
                if (string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("No refresh token available");
                    return false;
                }

                // Make refresh request
                var refreshRequest = new { refreshToken = refreshToken };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Refresh response: {Content}", responseContent);

                    // For now, assume refresh was successful if we get 200 OK
                    // In a real implementation, you'd parse the response and update tokens
                    _logger.LogInformation("JWT token refresh request successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("JWT token refresh request failed with status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh attempt");
                return false;
            }
        }

        /// <summary>
        /// Checks if the request URI is an authentication endpoint that shouldn't have authorization headers
        /// </summary>
        private bool IsAuthEndpoint(Uri? requestUri)
        {
            if (requestUri == null)
                return false;

            var absolutePath = requestUri.AbsolutePath.ToLowerInvariant();
            return absolutePath.Contains("/api/auth/login") ||
                   absolutePath.Contains("/api/auth/register") ||
                   absolutePath.Contains("/api/auth/refresh");
        }

        /// <summary>
        /// Clones an HttpRequestMessage for retry purposes
        /// </summary>
        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy headers
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content if present
            if (request.Content != null)
            {
                var content = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(content);

                // Copy content headers
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }
}
