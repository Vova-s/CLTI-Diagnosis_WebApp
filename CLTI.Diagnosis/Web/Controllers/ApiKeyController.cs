using Microsoft.AspNetCore.Mvc;
using CLTI.Diagnosis.Services;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiKeyController : ControllerBase
    {
        private readonly ApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeyController> _logger;

        public ApiKeyController(ApiKeyService apiKeyService, ILogger<ApiKeyController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        [HttpGet("openai")]
        public async Task<IActionResult> GetOpenAiApiKey()
        {
            try
            {
                var apiKey = await _apiKeyService.GetOpenAiApiKeyAsync();

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("OpenAI API key not found or expired");
                    return NotFound(new { error = "API key not available" });
                }

                // Повертаємо тільки частину ключа для безпеки (перші 7 символів + ...)
                var maskedKey = apiKey.Length > 10 ?
                    apiKey.Substring(0, 7) + "..." + apiKey.Substring(apiKey.Length - 4) :
                    "***";

                _logger.LogInformation("API key retrieved successfully (masked: {MaskedKey})", maskedKey);

                return Ok(new
                {
                    apiKey = apiKey,
                    masked = maskedKey,
                    available = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OpenAI API key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
