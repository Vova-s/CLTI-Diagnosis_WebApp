using System.Security.Claims;

namespace CLTI.Diagnosis.Services
{
    public interface IAuthenticatedHttpClientService
    {
        Task<HttpClient> CreateClientAsync();
    }

    public class AuthenticatedHttpClientService : IAuthenticatedHttpClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthenticatedHttpClientService> _logger;

        public AuthenticatedHttpClientService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthenticatedHttpClientService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HttpClient> CreateClientAsync()
        {
            var client = _httpClientFactory.CreateClient("Default");
            var context = _httpContextAccessor.HttpContext;

            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                // Передаємо cookies з поточного запиту
                var cookieHeader = context.Request.Headers["Cookie"].ToString();
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    client.DefaultRequestHeaders.Remove("Cookie");
                    client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                    _logger.LogDebug("Added authentication cookies to HTTP client");
                }

                // Додаємо інформацію про користувача в заголовки (опціонально)
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    client.DefaultRequestHeaders.Remove("X-User-Id");
                    client.DefaultRequestHeaders.Add("X-User-Id", userId);
                }
            }
            else
            {
                _logger.LogDebug("User not authenticated, creating client without auth headers");
            }

            return await Task.FromResult(client);
        }
    }
}