using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(HttpClient httpClient, IConfiguration configuration, ILogger<AiChatController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { Error = "Повідомлення не може бути порожнім" });
                }

                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new { Error = "API ключ не налаштований" });
                }

                // Підготовка запиту до OpenAI
                var openAiRequest = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new object[]
                    {
                        new
                        {
                            role = "system",
                            content = "Ти медичний асистент, що спеціалізується на діагностиці CLTI (Chronic Limb-Threatening Ischemia). " +
                                     "Ти допомагаєш лікарям з класифікаціями WiFI, CRAB, 2YLE та GLASS. " +
                                     "Відповідай українською мовою, будь точним та професійним."
                        },
                        new
                        {
                            role = "user",
                            content = request.Message
                        }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(openAiRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API Error: {response.StatusCode}, Content: {errorContent}");
                    return StatusCode(500, new { Error = "Помилка при зверненні до AI сервісу" });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseContent);

                var aiMessage = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "Вибачте, не вдалося отримати відповідь";

                return Ok(new ChatResponse
                {
                    Success = true,
                    Message = aiMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при обробці запиту до AI");
                return StatusCode(500, new { Error = "Внутрішня помилка сервера", Details = ex.Message });
            }
        }
    }

    // DTO класи
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Error { get; set; }
    }

    // Класи для роботи з OpenAI API
    public class OpenAiResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    public class Choice
    {
        public MessageContent? Message { get; set; }
    }

    public class MessageContent
    {
        public string? Content { get; set; }
    }
}