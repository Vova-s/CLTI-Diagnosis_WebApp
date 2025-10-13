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
                    _logger.LogDebug("Returning cached OpenAI API key. Cache age: {CacheAge} minutes", 
                        (DateTime.UtcNow - _lastCacheTime).TotalMinutes);
                    return _cachedApiKey;
                }

                _logger.LogInformation("Cache expired or empty. Fetching OpenAI API key from database");

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
                        
                        var expiryInfo = apiKeyEntity.ExpiresAt.HasValue 
                            ? $"Expires at: {apiKeyEntity.ExpiresAt.Value:yyyy-MM-dd HH:mm:ss}" 
                            : "No expiry date";
                        
                        _logger.LogInformation("OpenAI API key loaded from database successfully. {ExpiryInfo}", expiryInfo);
                        return _cachedApiKey;
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI API key found but expired. Expires at: {ExpiresAt}, Current time: {CurrentTime}", 
                            apiKeyEntity.ExpiresAt, DateTime.UtcNow);
                    }
                }
                else
                {
                    if (apiKeyEntity == null)
                    {
                        _logger.LogWarning("OpenAI API key entity not found in database (sys_api_key table, ID = 1)");
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI API key entity found but ApiKey field is empty or null");
                    }
                }

                return null;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while retrieving OpenAI API key. InnerException: {InnerException}", 
                    ex.InnerException?.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving OpenAI API key from database. Error type: {ErrorType}", 
                    ex.GetType().Name);
                return null;
            }
        }
    }
}
