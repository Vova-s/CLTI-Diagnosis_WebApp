using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CLTI.Diagnosis.Middleware
{
    public class AuthenticationDiagnosticMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationDiagnosticMiddleware> _logger;

        public AuthenticationDiagnosticMiddleware(RequestDelegate next, ILogger<AuthenticationDiagnosticMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Логуємо тільки для API запитів та автентифікаційних сторінок
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var shouldLog = path.StartsWith("/api") ||
                           path.IndexOf("/account/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           path.StartsWith("/_blazor");

            if (shouldLog)
            {
                await LogAuthenticationDetails(context, "BEFORE");
            }

            await _next(context);

            if (shouldLog)
            {
                await LogAuthenticationDetails(context, "AFTER");
            }
        }

        private async Task LogAuthenticationDetails(HttpContext context, string stage)
        {
            try
            {
                var request = context.Request;
                var user = context.User;

                var authInfo = new
                {
                    Stage = stage,
                    Path = request.Path.Value,
                    Method = request.Method,
                    IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,
                    AuthenticationType = user?.Identity?.AuthenticationType,
                    UserName = user?.Identity?.Name,
                    UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ClaimsCount = user?.Claims?.Count() ?? 0,
                    HasIdentityCookie = request.Cookies.ContainsKey(".AspNetCore.Identity.Application"),
                    CookiesCount = request.Cookies.Count,
                    UserAgent = request.Headers.UserAgent.ToString(),
                    RemoteIp = context.Connection.RemoteIpAddress?.ToString()
                };

                _logger.LogInformation("Auth {Stage}: Path={Path}, IsAuth={IsAuthenticated}, " +
                                     "AuthType={AuthenticationType}, UserId={UserId}, " +
                                     "HasCookie={HasIdentityCookie}, Claims={ClaimsCount}",
                    authInfo.Stage,
                    authInfo.Path,
                    authInfo.IsAuthenticated,
                    authInfo.AuthenticationType,
                    authInfo.UserId,
                    authInfo.HasIdentityCookie,
                    authInfo.ClaimsCount);

                // Детальне логування для проблемних випадків
                if (stage == "BEFORE" &&
                    request.Path.StartsWithSegments("/api") &&
                    (!user?.Identity?.IsAuthenticated ?? true))
                {
                    _logger.LogWarning("Unauthenticated API request detected");

                    // Логуємо всі cookies для діагностики
                    foreach (var cookie in request.Cookies)
                    {
                        if (cookie.Key.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            cookie.Key.IndexOf("Auth", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _logger.LogWarning("Auth Cookie: {Name} = {Value}",
                                cookie.Key,
                                cookie.Value?.Substring(0, Math.Min(30, cookie.Value.Length)) + "...");
                        }
                    }

                    // Логуємо claims (якщо є)
                    if (user?.Claims?.Any() == true)
                    {
                        foreach (var claim in user.Claims)
                        {
                            _logger.LogWarning("Claim: {Type} = {Value}", claim.Type, claim.Value);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No claims found in user principal");
                    }
                }

                // Логуємо Set-Cookie заголовки після автентифікації
                if (stage == "AFTER" &&
                    (request.Path.Value?.IndexOf("/Account/Login", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     context.Response.Headers.ContainsKey("Set-Cookie")))
                {
                    if (context.Response.Headers.ContainsKey("Set-Cookie"))
                    {
                        var setCookies = context.Response.Headers["Set-Cookie"];
                        foreach (var setCookie in setCookies)
                        {
                            if (setCookie.ToString().IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                _logger.LogInformation("Set-Cookie (Identity): {Cookie}",
                                    setCookie.ToString().Substring(0, Math.Min(100, setCookie.ToString().Length)) + "...");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication diagnostic middleware");
            }
        }
    }

    // Extension method для додавання middleware
    public static class AuthenticationDiagnosticMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationDiagnostic(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationDiagnosticMiddleware>();
        }
    }
}