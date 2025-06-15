using System.Net.Http.Json;

namespace CLTI.Diagnosis.Client.Services
{
    public interface IClientApiKeyService
    {
        Task<string?> GetOpenAiApiKeyAsync();
    }

    public class ClientApiKeyService : IClientApiKeyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClientApiKeyService> _logger;
        private string? _cachedApiKey;
        private DateTime _lastCacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

        public ClientApiKeyService(HttpClient httpClient, ILogger<ClientApiKeyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> GetOpenAiApiKeyAsync()
        {
            try
            {
                // Перевіряємо кеш
                if (!string.IsNullOrEmpty(_cachedApiKey) &&
                    DateTime.UtcNow - _lastCacheTime < _cacheExpiry)
                {
                    return _cachedApiKey;
                }

                // Отримуємо ключ через API
                var response = await _httpClient.GetAsync("/api/apikey/openai");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiKeyResponse>();

                    if (result != null && !string.IsNullOrEmpty(result.ApiKey))
                    {
                        _cachedApiKey = result.ApiKey;
                        _lastCacheTime = DateTime.UtcNow;

                        _logger.LogInformation("OpenAI API key retrieved successfully from server");
                        return _cachedApiKey;
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to retrieve API key from server. Status: {StatusCode}",
                        response.StatusCode);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OpenAI API key from server");
                return null;
            }
        }

        private class ApiKeyResponse
        {
            public string ApiKey { get; set; } = string.Empty;
            public string Masked { get; set; } = string.Empty;
            public bool Available { get; set; }
        }
    }
}