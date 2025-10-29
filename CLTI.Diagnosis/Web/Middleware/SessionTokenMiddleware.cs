using CLTI.Diagnosis.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace CLTI.Diagnosis.Web.Middleware;

/// <summary>
/// Middleware that automatically adds JWT token from server-side session to Authorization header
/// Works for both Blazor Server requests and API calls from server-side services
/// </summary>
public class SessionTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionTokenMiddleware> _logger;

    public SessionTokenMiddleware(RequestDelegate next, ILogger<SessionTokenMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionStorageService sessionStorage)
    {
        // Only process if we're making an internal API call (has /api/ path)
        // AND the Authorization header is not already set
        if (context.Request.Path.StartsWithSegments("/api") && 
            !context.Request.Headers.ContainsKey("Authorization"))
        {
            try
            {
                var sessionId = sessionStorage.GetSessionId();
                _logger.LogDebug("SessionTokenMiddleware: Processing API request | Path: {Path} | SessionId: {SessionId}", 
                    context.Request.Path, sessionId);
                
                // Get token from server-side session
                var token = await sessionStorage.GetTokenAsync();
                
                if (!string.IsNullOrEmpty(token))
                {
                    // Add to request headers for this request only (doesn't persist)
                    context.Request.Headers.Append("Authorization", $"Bearer {token}");
                    _logger.LogInformation("✅ SessionTokenMiddleware: Added session token to request | Path: {Path} | SessionId: {SessionId} | TokenLength: {TokenLength}", 
                        context.Request.Path, sessionId, token.Length);
                }
                else
                {
                    _logger.LogWarning("⚠️ SessionTokenMiddleware: No session token found | Path: {Path} | SessionId: {SessionId}", 
                        context.Request.Path, sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SessionTokenMiddleware: Error getting session token | Path: {Path}", context.Request.Path);
                // Continue without token - authentication will fail but we won't break the request
            }
        }

        await _next(context);
    }
}

