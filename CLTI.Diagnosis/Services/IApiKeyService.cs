using CLTI.Diagnosis.Data;
using Microsoft.EntityFrameworkCore;

namespace CLTI.Diagnosis.Services
{
    public interface IApiKeyService
    {
        Task<string?> GetOpenAiApiKeyAsync();
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiKeyService> _logger;
        private string? _cachedApiKey;
        private DateTime _lastCacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30); // Кешуємо на 30 хвилин

        public ApiKeyService(ApplicationDbContext context, ILogger<ApiKeyService> logger)
        {
            _context = context;
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

                // Отримуємо ключ з бази даних
                var apiKeyEntity = await _context.SysApiKeys
                    .Where(k => k.Id == 1)
                    .FirstOrDefaultAsync();

                if (apiKeyEntity != null && !string.IsNullOrEmpty(apiKeyEntity.ApiKey))
                {
                    // Перевіряємо чи ключ не закінчився
                    if (apiKeyEntity.ExpiresAt == null || apiKeyEntity.ExpiresAt > DateTime.UtcNow)
                    {
                        _cachedApiKey = apiKeyEntity.ApiKey;
                        _lastCacheTime = DateTime.UtcNow;

                        _logger.LogInformation("OpenAI API key loaded from database successfully");
                        return _cachedApiKey;
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI API key found but expired. Expires at: {ExpiresAt}",
                            apiKeyEntity.ExpiresAt);
                    }
                }
                else
                {
                    _logger.LogWarning("OpenAI API key not found in database (sys_api_key table, ID = 1)");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OpenAI API key from database");
                return null;
            }
        }
    }
}