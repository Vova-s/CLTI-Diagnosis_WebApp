using System.Net;
using System.Text.Json;

namespace CLTI.Diagnosis.Middleware
{
    /// <summary>
    /// Global exception handling middleware that logs all unhandled exceptions
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unhandled exception occurred. Path: {Path}, Method: {Method}, RemoteIp: {RemoteIp}, User: {User}, ErrorType: {ErrorType}",
                    context.Request.Path,
                    context.Request.Method,
                    context.Connection.RemoteIpAddress,
                    context.User?.Identity?.Name ?? "Anonymous",
                    ex.GetType().Name);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An internal server error occurred",
                Error = _environment.IsDevelopment() ? exception.Message : "Internal server error",
                ErrorType = _environment.IsDevelopment() ? exception.GetType().Name : null,
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                Path = context.Request.Path.Value,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("Sending error response: {@ErrorResponse}", response);

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Extension method to register the global exception handler middleware
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}
