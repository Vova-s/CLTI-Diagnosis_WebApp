using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.Json;

namespace CLTI.Diagnosis.Infrastructure.Services;

/// <summary>
/// Service for storing authentication data in server-side session storage
/// Uses IDistributedCache (can be in-memory, SQL Server, Redis) - DOES NOT require cookies
/// Works even if user blocks cookies (uses session ID in URL or header)
/// </summary>
public interface ISessionStorageService
{
    /// <summary>
    /// Stores JWT token in server-side session
    /// </summary>
    Task SetTokenAsync(string token, bool rememberMe = false, int? userId = null);

    /// <summary>
    /// Gets JWT token from server-side session
    /// </summary>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Stores refresh token in server-side session
    /// </summary>
    Task SetRefreshTokenAsync(string refreshToken, bool rememberMe = false, int? userId = null);

    /// <summary>
    /// Gets refresh token from server-side session
    /// </summary>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Stores user data in server-side session
    /// </summary>
    Task SetUserAsync<T>(T user) where T : class;

    /// <summary>
    /// Gets user data from server-side session
    /// </summary>
    Task<T?> GetUserAsync<T>() where T : class;

    /// <summary>
    /// Clears all session data
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Stores userId in session for later retrieval
    /// </summary>
    void SetUserId(int userId);

    /// <summary>
    /// Gets session ID (can be used as fallback identifier if cookies blocked)
    /// </summary>
    string GetSessionId();
}

public class SessionStorageService : ISessionStorageService
{
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SessionStorageService> _logger;

    // Cache keys
    private const string TOKEN_KEY = "session:auth:token";
    private const string REFRESH_TOKEN_KEY = "session:auth:refresh";
    private const string USER_KEY = "session:auth:user";
    private const string USER_ID_KEY = "session:auth:userId"; // For session-based userId mapping

    public SessionStorageService(
        IDistributedCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SessionStorageService> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    /// <summary>
    /// Gets unique session identifier - works even without cookies
    /// </summary>
    private string GetSessionKey(string key, int? userId = null)
    {
        // ✅ PRIMARY KEY: Use userId only (survives server restarts)
        // Session ID is used for additional isolation within same user
        if (userId.HasValue)
        {
            // Primary key: user:9:token (survives server restart)
            return $"user:{userId}:{key}";
        }
        
        // Fallback: use session ID if no userId (shouldn't happen after login)
        var sessionId = GetSessionId();
        return $"{sessionId}:{key}";
    }

    /// <summary>
    /// Try to get userId from current user claims, session, or distributed cache
    /// </summary>
    private async Task<int?> GetUserIdAsync()
    {
        try
        {
            // First try from authenticated user claims (if user is already authenticated)
            var userIdClaim = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // ✅ Second: try to get userId from session (stored during login)
            if (HttpContext?.Session != null)
            {
                try
                {
                    // Ensure session is loaded - touch it first
                    HttpContext.Session.SetString("_touch", "1");
                    var sessionUserId = HttpContext.Session.GetString("_userId");
                    if (!string.IsNullOrEmpty(sessionUserId) && int.TryParse(sessionUserId, out var sessionUserIdInt))
                    {
                        _logger.LogDebug("GetUserId: Found userId in session: {UserId}", sessionUserIdInt);
                        return sessionUserIdInt;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetUserId: Error reading from session");
                }
            }

            // ✅ Third: try to get userId from distributed cache using session ID as key
            // This works even if session cookie was lost but we have session ID
            var sessionId = GetSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    var cachedUserId = await _cache.GetStringAsync($"{sessionId}:{USER_ID_KEY}");
                    if (!string.IsNullOrEmpty(cachedUserId) && int.TryParse(cachedUserId, out var cachedUserIdInt))
                    {
                        _logger.LogDebug("GetUserId: Found userId in distributed cache: {UserId}", cachedUserIdInt);
                        // Restore to session for faster access
                        if (HttpContext?.Session != null)
                        {
                            HttpContext.Session.SetString("_userId", cachedUserId);
                        }
                        return cachedUserIdInt;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "GetUserId: Error reading from distributed cache");
                }
            }

            // ✅ Fourth: try to extract userId from Authorization header if present (before checking cookie)
            // If client has a JWT token (even from localStorage), we can extract userId from it
            // This helps when user already has a valid JWT token but userId wasn't found in session/cache
            var authHeader = HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
                    var parts = jwtToken.Split('.');
                    if (parts.Length == 3)
                    {
                        var payload = parts[1];
                        // Add padding if needed for Base64 decoding
                        payload = payload.PadRight((payload.Length + 3) & ~3, '=');
                        var payloadBytes = Convert.FromBase64String(payload);
                        var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
                        var payloadData = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                        
                        // Try different claim names for userId
                        string? extractedUserId = null;
                        if (payloadData.TryGetProperty("nameid", out var nameidElement))
                        {
                            extractedUserId = nameidElement.GetString();
                        }
                        else if (payloadData.TryGetProperty("sub", out var subElement))
                        {
                            extractedUserId = subElement.GetString();
                        }
                        else if (payloadData.TryGetProperty(ClaimTypes.NameIdentifier, out var nameIdElement))
                        {
                            extractedUserId = nameIdElement.GetString();
                        }
                        else if (payloadData.TryGetProperty("user_id", out var userIdElement))
                        {
                            extractedUserId = userIdElement.GetString();
                        }
                        
                        if (!string.IsNullOrEmpty(extractedUserId) && int.TryParse(extractedUserId, out var userIdFromToken))
                        {
                            _logger.LogInformation("✅ GetUserId: Extracted userId from JWT token in Authorization header: {UserId}", userIdFromToken);
                            
                            // Store in session and cache for faster access next time
                            if (HttpContext?.Session != null)
                            {
                                HttpContext.Session.SetString("_userId", userIdFromToken.ToString());
                            }
                            
                            // Also store in distributed cache
                            if (!string.IsNullOrEmpty(sessionId))
                            {
                                var userIdCacheKey = $"{sessionId}:{USER_ID_KEY}";
                                await _cache.SetStringAsync(userIdCacheKey, userIdFromToken.ToString(), 
                                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
                            }
                            
                            // ✅ Also set cookie for future requests
                            if (HttpContext?.Response != null)
                            {
                                var request = HttpContext.Request;
                                var cookieOptions = new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = request.IsHttps,
                                    SameSite = SameSiteMode.Lax,
                                    IsEssential = true,
                                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                                    Path = "/",
                                    Domain = null
                                };
                                HttpContext.Response.Cookies.Append("_userId", userIdFromToken.ToString(), cookieOptions);
                                _logger.LogDebug("GetUserId: Restored userId cookie from JWT token");
                            }
                            
                            return userIdFromToken;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "GetUserId: Failed to extract userId from Authorization header");
                }
            }

            // ✅ Fifth: try to get userId from cookie (survives server restarts even if session changes)
            // This is a fallback when session ID changed after server restart
            if (HttpContext?.Request.Cookies != null)
            {
                try
                {
                    var allCookies = HttpContext.Request.Cookies.Keys.ToList();
                    _logger.LogWarning("🔍 GetUserId: Checking cookies after server restart. Available cookies: {Cookies} | Count: {Count}", 
                        string.Join(", ", allCookies), allCookies.Count);
                    
                    var userIdCookie = HttpContext.Request.Cookies["_userId"];
                    _logger.LogDebug("GetUserId: userId cookie value: {CookieValue}", userIdCookie ?? "NULL");
                    
                    if (!string.IsNullOrEmpty(userIdCookie) && int.TryParse(userIdCookie, out var userIdFromCookie))
                    {
                        _logger.LogInformation("✅ GetUserId: Found userId in cookie: {UserId}", userIdFromCookie);
                        
                        // Restore to session and cache for faster access
                        if (HttpContext.Session != null)
                        {
                            HttpContext.Session.SetString("_userId", userIdFromCookie.ToString());
                            _logger.LogDebug("GetUserId: Restored userId to session");
                        }
                        
                        if (!string.IsNullOrEmpty(sessionId))
                        {
                            var userIdCacheKey = $"{sessionId}:{USER_ID_KEY}";
                            await _cache.SetStringAsync(userIdCacheKey, userIdFromCookie.ToString(), 
                                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
                            _logger.LogDebug("GetUserId: Stored userId in cache with new session ID");
                        }
                        
                        return userIdFromCookie;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ GetUserId: userId cookie not found or invalid. Value: {Value}", userIdCookie ?? "NULL");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetUserId: Error reading userId from cookie");
                }
            }
            else
            {
                _logger.LogDebug("GetUserId: Cookies not available");
            }
        }
        catch
        {
            // Ignore - user might not be authenticated
        }
        return null;
    }

    /// <summary>
    /// Synchronous version for backward compatibility (uses distributed cache)
    /// </summary>
    private int? GetUserId()
    {
        // Note: Cannot use async in synchronous method, so we read from session only
        try
        {
            var userIdClaim = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            if (HttpContext?.Session != null)
            {
                var sessionUserId = HttpContext.Session.GetString("_userId");
                if (!string.IsNullOrEmpty(sessionUserId) && int.TryParse(sessionUserId, out var sessionUserIdInt))
                {
                    return sessionUserIdInt;
                }
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }

    /// <summary>
    /// Store userId in session, cache, and cookie for persistence across server restarts
    /// </summary>
    public async Task SetUserIdAsync(int userId)
    {
        try
        {
            var sessionId = GetSessionId();
            
            // 1. Store in session (fast access)
            if (HttpContext?.Session != null)
            {
                HttpContext.Session.SetString("_userId", userId.ToString());
                await HttpContext.Session.CommitAsync();
                _logger.LogDebug("SetUserId: Stored userId in session: {UserId}", userId);
            }
            
            // 2. Store in distributed cache (survives server restarts if session ID matches)
            if (!string.IsNullOrEmpty(sessionId))
            {
                var userIdCacheKey = $"{sessionId}:{USER_ID_KEY}";
                await _cache.SetStringAsync(userIdCacheKey, userId.ToString(), 
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
                _logger.LogDebug("SetUserId: Stored userId in cache: {UserId}, SessionId: {SessionId}", userId, sessionId);
            }
            
            // 3. ✅ Store in cookie (survives server restarts even if session ID changes)
            if (HttpContext?.Response != null)
            {
                var request = HttpContext.Request;
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true, // ✅ Security: not accessible via JavaScript
                    Secure = request.IsHttps, // ✅ Match request scheme (HTTPS/HTTP)
                    SameSite = SameSiteMode.Lax, // ✅ CSRF protection
                    IsEssential = true, // ✅ Required for functionality
                    Expires = DateTimeOffset.UtcNow.AddDays(30), // ✅ Long expiration
                    Path = "/", // ✅ Important: Set path to root so cookie is available site-wide
                    // ✅ Explicitly set domain for localhost (allows both localhost and 127.0.0.1)
                    Domain = null // Let browser determine (works for localhost)
                };
                
                try
                {
                    var cookieValue = userId.ToString();
                    HttpContext.Response.Cookies.Append("_userId", cookieValue, cookieOptions);
                    
                    // ✅ Verify cookie was set by checking response headers
                    var setCookieHeaders = HttpContext.Response.Headers["Set-Cookie"].ToList();
                    var userIdCookieInHeader = setCookieHeaders.Any(h => h != null && h.Contains("_userId"));
                    
                    _logger.LogInformation("✅ SetUserId (async): Stored userId in cookie: {UserId} | Cookie expires: {Expires} | Secure: {Secure} | Path: {Path} | IsHttps: {IsHttps} | In headers: {InHeaders}", 
                        userId, cookieOptions.Expires, cookieOptions.Secure, cookieOptions.Path, request.IsHttps, userIdCookieInHeader);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ SetUserId (async): Failed to set cookie | Error: {Error}", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error storing userId");
        }
    }

    /// <summary>
    /// Store userId in session, cache, and cookie (synchronous version - for backward compatibility)
    /// NOTE: Cookie setting is now done directly in AuthController to avoid conflicts
    /// This method only stores in session
    /// </summary>
    public void SetUserId(int userId)
    {
        try
        {
            var sessionId = GetSessionId();
            
            // 1. Store in session only (cookie is set separately in AuthController)
            if (HttpContext?.Session != null)
            {
                HttpContext.Session.SetString("_userId", userId.ToString());
                _logger.LogDebug("SetUserId: Stored userId in session: {UserId}", userId);
            }
            
            // Note: Cookie is now set directly in AuthController before this method is called
            // This avoids double-setting and ensures cookie is in response headers
            // Cache update is async, so we skip it in sync version
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error storing userId");
        }
    }

    public string GetSessionId()
    {
        if (HttpContext?.Session == null)
        {
            // Fallback: generate a temporary ID if session is not available
            var tempId = Guid.NewGuid().ToString("N");
            _logger.LogWarning("Session not available, using temporary ID: {TempId}", tempId);
            return tempId;
        }

        try
        {
            // Ensure session is loaded - this will create a new session if cookie is invalid
            // Session middleware handles cookie decryption and creates new session if needed
            HttpContext.Session.SetString("_initialized", "1"); // Touch session to ensure it's initialized
            
            // Use ASP.NET Core's session ID - it handles cookie encryption/decryption automatically
            // If cookie is invalid, ASP.NET Core will create a new session with new ID
            var sessionId = HttpContext.Session.Id;
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }
        }
        catch (Exception ex)
        {
            // If session can't be accessed (e.g., cookie decryption failed), log and fallback
            _logger.LogWarning(ex, "Error accessing session, will generate new session ID");
        }

        // Last resort: generate new ID if session ID is empty or session failed
        var newSessionId = Guid.NewGuid().ToString("N");
        _logger.LogWarning("Session ID unavailable, generating new ID: {NewSessionId}", newSessionId);
        return newSessionId;
    }

    public async Task SetTokenAsync(string token, bool rememberMe = false, int? userId = null)
    {
        try
        {
            // Try to get userId from token if not provided
            if (!userId.HasValue)
            {
                userId = GetUserId();
            }

            var sessionId = GetSessionId();
            var cacheKey = GetSessionKey(TOKEN_KEY, userId);
            
            // ✅ Longer expiration for better UX - tokens survive server restarts (SQL Server cache)
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = rememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromDays(7)
            };

            // ✅ Store token with userId as primary key (survives server restart)
            await _cache.SetStringAsync(cacheKey, token, options);
            
            // ✅ ALSO store userId in distributed cache mapped to session ID (for recovery)
            if (userId.HasValue)
            {
                var userIdCacheKey = $"{sessionId}:{USER_ID_KEY}";
                await _cache.SetStringAsync(userIdCacheKey, userId.Value.ToString(), 
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
                _logger.LogDebug("SetTokenAsync: UserId also stored in cache with session mapping | SessionId: {SessionId}, UserId: {UserId}", 
                    sessionId, userId);
            }
            
            _logger.LogInformation("✅ SetTokenAsync: Token stored | SessionId: {SessionId}, UserId: {UserId}, CacheKey: {CacheKey}, TokenLength: {TokenLength}", 
                sessionId, userId, cacheKey, token.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing token in session");
            throw;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            // ✅ Use async version to check distributed cache for userId
            var userId = await GetUserIdAsync();
            
            if (!userId.HasValue)
            {
                _logger.LogWarning("⚠️ GetTokenAsync: userId is null - user may not be logged in or session expired | SessionId: {SessionId}", sessionId);
                // Cannot find token without userId - user needs to login
                return null;
            }
            
            // ✅ First try with userId (primary key - survives server restarts)
            var cacheKey = GetSessionKey(TOKEN_KEY, userId);
            _logger.LogDebug("GetTokenAsync: SessionId={SessionId}, UserId={UserId}, CacheKey={CacheKey}", 
                sessionId, userId, cacheKey);
            
            var token = await _cache.GetStringAsync(cacheKey);
            
            // ✅ If token not found and we have userId, the cacheKey already uses userId, so no fallback needed
            // The primary storage is already by userId, so it survives server restarts
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("⚠️ GetTokenAsync: Token not found in cache | UserId: {UserId}, CacheKey: {CacheKey}. User may need to login again.", 
                    userId, cacheKey);
            }
            
            // This fallback is no longer needed since GetUserIdAsync already extracts from Authorization header
            if (false && string.IsNullOrEmpty(token) && !userId.HasValue)
            {
                // ✅ Last resort: try to extract userId from Authorization header if present
                var authHeader = HttpContext?.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
                        // Try to decode userId from JWT token (without validation, just for session recovery)
                        var parts = jwtToken.Split('.');
                        if (parts.Length == 3)
                        {
                            var payload = parts[1];
                            // Add padding if needed
                            payload = payload.PadRight((payload.Length + 3) & ~3, '=');
                            var payloadBytes = Convert.FromBase64String(payload);
                            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
                            var payloadData = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                            
                            if (payloadData.TryGetProperty("nameid", out var nameidElement))
                            {
                                var extractedUserId = nameidElement.GetString();
                                if (!string.IsNullOrEmpty(extractedUserId) && int.TryParse(extractedUserId, out var userIdFromToken))
                                {
                                    _logger.LogDebug("GetTokenAsync: Extracted userId from JWT token: {UserId}", userIdFromToken);
                                    
                                    // Try fallback key with extracted userId
                                    var fallbackKey = $"user:{userIdFromToken}:{TOKEN_KEY}";
                                    token = await _cache.GetStringAsync(fallbackKey);
                                    
                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        _logger.LogInformation("✅ GetTokenAsync: Token found using userId from JWT token | UserId: {UserId}", userIdFromToken);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "GetTokenAsync: Failed to extract userId from Authorization header");
                    }
                }
            }
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("GetTokenAsync: No token found in cache | SessionId: {SessionId}, UserId: {UserId}, CacheKey: {CacheKey}", 
                    sessionId, userId, cacheKey);
            }
            else
            {
                _logger.LogDebug("GetTokenAsync: Token found | SessionId: {SessionId}, UserId: {UserId}, TokenLength: {TokenLength}", 
                    sessionId, userId, token.Length);
            }
            
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token from session");
            return null;
        }
    }

    public async Task SetRefreshTokenAsync(string refreshToken, bool rememberMe = false, int? userId = null)
    {
        try
        {
            // Try to get userId if not provided
            if (!userId.HasValue)
            {
                userId = GetUserId();
            }

            var sessionId = GetSessionId();
            var cacheKey = GetSessionKey(REFRESH_TOKEN_KEY, userId);
            
            // ✅ Longer expiration for refresh tokens - survive server restarts
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = rememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromDays(14)
            };

            // ✅ Store refresh token with userId as primary key (survives server restart)
            await _cache.SetStringAsync(cacheKey, refreshToken, options);
            
            // ✅ ALSO store userId in distributed cache mapped to session ID (for recovery)
            if (userId.HasValue)
            {
                var userIdCacheKey = $"{sessionId}:{USER_ID_KEY}";
                await _cache.SetStringAsync(userIdCacheKey, userId.Value.ToString(), 
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) });
            }
            
            _logger.LogInformation("✅ SetRefreshTokenAsync: Refresh token stored | SessionId: {SessionId}, UserId: {UserId}, CacheKey: {CacheKey}", 
                sessionId, userId, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing refresh token in session");
            throw;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            // ✅ Use async version to check distributed cache for userId
            var userId = await GetUserIdAsync();
            
            // ✅ First try with current session ID
            var cacheKey = GetSessionKey(REFRESH_TOKEN_KEY, userId);
            _logger.LogDebug("GetRefreshTokenAsync: SessionId={SessionId}, UserId={UserId}, CacheKey={CacheKey}", 
                sessionId, userId, cacheKey);
            
            var refreshToken = await _cache.GetStringAsync(cacheKey);
            
            // ✅ Cache key already uses userId, so it survives server restarts automatically
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("GetRefreshTokenAsync: No refresh token found in cache | SessionId: {SessionId}, UserId: {UserId}, CacheKey: {CacheKey}", 
                    sessionId, userId, cacheKey);
            }
            else
            {
                _logger.LogDebug("GetRefreshTokenAsync: Refresh token found | SessionId: {SessionId}, UserId: {UserId}", 
                    sessionId, userId);
            }
            
            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh token from session");
            return null;
        }
    }

    public async Task SetUserAsync<T>(T user) where T : class
    {
        try
        {
            var userJson = JsonSerializer.Serialize(user);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) // User data persists longer
            };

            await _cache.SetStringAsync(GetSessionKey(USER_KEY), userJson, options);
            _logger.LogDebug("User data stored in server session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing user in session");
            throw;
        }
    }

    public async Task<T?> GetUserAsync<T>() where T : class
    {
        try
        {
            var userJson = await _cache.GetStringAsync(GetSessionKey(USER_KEY));
            if (string.IsNullOrEmpty(userJson))
                return null;

            return JsonSerializer.Deserialize<T>(userJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user from session");
            return null;
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            var sessionId = GetSessionId();
            var userId = await GetUserIdAsync();
            
            // Clear tokens from cache (using userId-based keys)
            await _cache.RemoveAsync(GetSessionKey(TOKEN_KEY, userId));
            await _cache.RemoveAsync(GetSessionKey(REFRESH_TOKEN_KEY, userId));
            await _cache.RemoveAsync(GetSessionKey(USER_KEY, userId));
            
            // Clear userId mapping from cache
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _cache.RemoveAsync($"{sessionId}:{USER_ID_KEY}");
            }
            
            // Clear userId from session
            HttpContext?.Session?.Remove("_userId");
            
            // Clear userId cookie
            if (HttpContext?.Response != null)
            {
                HttpContext.Response.Cookies.Delete("_userId");
            }
            
            _logger.LogDebug("All session data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session data");
        }
    }
}

