using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DiagnosticsController(ILogger<DiagnosticsController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            try
            {
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

                _logger.LogInformation("Health check successful: {@HealthInfo}", info);
                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        [HttpGet("test-connection")]
        public IActionResult TestConnection([FromServices] IHttpClientFactory httpClientFactory)
        {
            try
            {
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

                _logger.LogInformation("Connection test: {@ConnectionInfo}", connectionInfo);
                return Ok(connectionInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("test-echo")]
        public IActionResult TestEcho([FromBody] object data)
        {
            try
            {
                var response = new
                {
                    ReceivedAt = DateTime.UtcNow,
                    Data = data,
                    ContentType = Request.ContentType,
                    ContentLength = Request.ContentLength,
                    Success = true
                };

                _logger.LogInformation("Echo test successful: {@EchoResponse}", response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Echo test failed");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("environment")]
        public IActionResult GetEnvironmentInfo()
        {
            try
            {
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

                return Ok(env);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Environment info failed");
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}