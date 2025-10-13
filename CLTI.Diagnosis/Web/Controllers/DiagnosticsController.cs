using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using CLTI.Diagnosis.Data;
using Microsoft.EntityFrameworkCore;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public DiagnosticsController(
            ILogger<DiagnosticsController> logger, 
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _logger = logger;
            _environment = environment;
            _context = context;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
                _logger.LogInformation("Health check endpoint called from {RemoteIp}", 
                    HttpContext.Connection.RemoteIpAddress);

                var info = new
                {
                    Status = "Healthy",
                    Environment = _environment.EnvironmentName,
                    Machine = System.Environment.MachineName,
                    Timestamp = DateTime.UtcNow,
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    ProcessId = System.Environment.ProcessId,
                    WorkingSet = GC.GetTotalMemory(false),
                    BaseDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    ContentRoot = _environment.ContentRootPath,
                    WebRoot = _environment.WebRootPath
                };

                _logger.LogInformation("Health check successful: Status={Status}, Environment={Environment}, Machine={Machine}", 
                    info.Status, info.Environment, info.Machine);
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        [HttpGet("health/database")]
        public async Task<IActionResult> DatabaseHealth()
        {
            try
            {
                _logger.LogInformation("Database health check called from {RemoteIp}", 
                    HttpContext.Connection.RemoteIpAddress);

                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    _logger.LogWarning("Database connection failed");
                    return StatusCode(503, new 
                    { 
                        Status = "Unhealthy", 
                        Database = "Cannot connect",
                        Timestamp = DateTime.UtcNow 
                    });
                }

                // Test query to verify database is responsive
                var userCount = await _context.SysUsers.CountAsync();
                var caseCount = await _context.CltiCases.CountAsync();

                var dbInfo = new
                {
                    Status = "Healthy",
                    Database = "Connected",
                    ConnectionString = _context.Database.GetConnectionString()?.Split(';')[0], // Only show server part
                    UserCount = userCount,
                    CaseCount = caseCount,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Database health check successful. Users: {UserCount}, Cases: {CaseCount}", 
                    userCount, caseCount);
                
                return Ok(dbInfo);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database health check failed with DbUpdateException. InnerException: {InnerException}", 
                    ex.InnerException?.Message);
                return StatusCode(503, new 
                { 
                    Status = "Unhealthy", 
                    Database = "Error",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(503, new 
                { 
                    Status = "Unhealthy", 
                    Database = "Error",
                    Error = ex.Message 
                });
            }
        }

        [HttpGet("test-connection")]
        public IActionResult TestConnection([FromServices] IHttpClientFactory httpClientFactory)
        {
            try
            {
                _logger.LogInformation("Connection test called from {RemoteIp}", 
                    HttpContext.Connection.RemoteIpAddress);

                var request = HttpContext.Request;

                // Тестуємо HttpClient конфігурацію
                var client = httpClientFactory.CreateClient("InternalApi");

                var connectionInfo = new
                {
                    Scheme = request.Scheme,
                    Host = request.Host.Value,
                    Path = request.Path.Value,
                    QueryString = request.QueryString.Value,
                    Method = request.Method,
                    CurrentUrl = $"{request.Scheme}://{request.Host}",
                    HttpClientBaseAddress = client.BaseAddress?.ToString(),
                    Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    RemoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    LocalIpAddress = HttpContext.Connection.LocalIpAddress?.ToString(),
                    IsAuthenticated = HttpContext.User?.Identity?.IsAuthenticated ?? false,
                    UserName = HttpContext.User?.Identity?.Name,
                    Claims = HttpContext.User?.Claims?.Select(c => new { c.Type, c.Value }).ToList()
                };

                _logger.LogInformation("Connection test successful. RemoteIp: {RemoteIp}, IsAuthenticated: {IsAuthenticated}", 
                    connectionInfo.RemoteIpAddress, connectionInfo.IsAuthenticated);
                return Ok(connectionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("test-echo")]
        public IActionResult TestEcho([FromBody] object data)
        {
            try
            {
                _logger.LogInformation("Echo test called from {RemoteIp} with ContentType: {ContentType}", 
                    HttpContext.Connection.RemoteIpAddress, Request.ContentType);

                var response = new
                {
                    ReceivedAt = DateTime.UtcNow,
                    Data = data,
                    ContentType = Request.ContentType,
                    ContentLength = Request.ContentLength,
                    Success = true
                };

                _logger.LogInformation("Echo test successful. ContentLength: {ContentLength}", 
                    Request.ContentLength);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echo test failed. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("environment")]
        public IActionResult GetEnvironmentInfo()
        {
            try
            {
                _logger.LogInformation("Environment info requested from {RemoteIp}", 
                    HttpContext.Connection.RemoteIpAddress);

                var env = new
                {
                    EnvironmentName = _environment.EnvironmentName,
                    IsDevelopment = _environment.IsDevelopment(),
                    IsProduction = _environment.IsProduction(),
                    IsStaging = _environment.IsStaging(),
                    ApplicationName = _environment.ApplicationName,
                    ContentRootPath = _environment.ContentRootPath,
                    WebRootPath = _environment.WebRootPath,
                    EnvironmentVariables = new
                    {
                        ASPNETCORE_ENVIRONMENT = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        ASPNETCORE_URLS = System.Environment.GetEnvironmentVariable("ASPNETCORE_URLS"),
                        DOTNET_ENVIRONMENT = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    }
                };

                _logger.LogInformation("Environment info retrieved. Environment: {Environment}, IsDevelopment: {IsDevelopment}", 
                    env.EnvironmentName, env.IsDevelopment);
                return Ok(env);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Environment info failed. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("logs/recent")]
        public IActionResult GetRecentLogs([FromQuery] int count = 50)
        {
            try
            {
                _logger.LogInformation("Recent logs requested. Count: {Count}, RemoteIp: {RemoteIp}", 
                    count, HttpContext.Connection.RemoteIpAddress);

                // This is a placeholder - in production you would read from actual log files or log storage
                var logInfo = new
                {
                    Message = "Log retrieval endpoint - implement based on your logging infrastructure",
                    RequestedCount = count,
                    Timestamp = DateTime.UtcNow,
                    Note = "Configure this endpoint to read from your log storage (file, database, or external service)"
                };

                return Ok(logInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recent logs. Error type: {ErrorType}", ex.GetType().Name);
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
