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
                    _logger.LogDebug("Using cached API key");
                    return _cachedApiKey;
                }

                _logger.LogInformation("Requesting API key from server");

                // Отримуємо ключ через API
                var response = await _httpClient.GetAsync("/api/apikey/openai");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiKeyResponse>();

                    if (result != null && !string.IsNullOrEmpty(result.ApiKey) && result.Available)
                    {
                        _cachedApiKey = result.ApiKey;
                        _lastCacheTime = DateTime.UtcNow;

                        _logger.LogInformation("OpenAI API key retrieved successfully from server (masked: {MaskedKey})",
                            result.Masked);
                        return _cachedApiKey;
                    }
                    else
                    {
                        _logger.LogWarning("Server returned invalid API key response");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("API key not found on server. Status: {StatusCode}", response.StatusCode);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to retrieve API key from server. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);
                }

                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while retrieving API key from server");
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while retrieving API key from server");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving OpenAI API key from server");
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