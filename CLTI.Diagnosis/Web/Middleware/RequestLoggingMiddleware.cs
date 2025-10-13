using System.Diagnostics;

namespace CLTI.Diagnosis.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to response headers for tracking
            context.Response.Headers.Append("X-Request-ID", requestId);

            try
            {
                _logger.LogInformation(
                    "HTTP Request started: {RequestId} {Method} {Path} from {RemoteIp} User: {User}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress,
                    context.User?.Identity?.Name ?? "Anonymous");

                await _next(context);

                stopwatch.Stop();

                _logger.LogInformation(
                    "HTTP Request completed: {RequestId} {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex,
                    "HTTP Request failed: {RequestId} {Method} {Path} failed after {ElapsedMs}ms with error: {ErrorMessage}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw; // Re-throw to be handled by global exception handler
            }
        }
    }

    /// <summary>
    /// Extension method to register the request logging middleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
