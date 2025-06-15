using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using CLTI.Diagnosis.Services;

namespace CLTI.Diagnosis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiKeyService _apiKeyService;
        private readonly ILogger<AiChatController> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AiChatController(
            IHttpClientFactory httpClientFactory,
            ApiKeyService apiKeyService,
            ILogger<AiChatController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiKeyService = apiKeyService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Message))
                {
                    return BadRequest(new ChatResponse
                    {
                        Success = false,
                        Error = "Повідомлення не може бути порожнім"
                    });
                }

                _logger.LogInformation("Received chat request: {Message}", request.Message);

                // Отримуємо API ключ з бази даних
                var apiKey = await _apiKeyService.GetOpenAiApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("OpenAI API key not available");
                    return StatusCode(500, new ChatResponse
                    {
                        Success = false,
                        Error = "AI сервіс тимчасово недоступний. Зверніться до адміністратора."
                    });
                }

                // Створюємо HTTP клієнт для OpenAI
                using var openAiClient = _httpClientFactory.CreateClient("OpenAI");
                openAiClient.DefaultRequestHeaders.Clear();
                openAiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                openAiClient.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis/1.0");

                // Підготовуємо повідомлення
                var messages = new List<object>();

                // Системне повідомлення
                messages.Add(new
                {
                    role = "system",
                    content = @"Ти - AI асистент для медичної системи діагностики CLTI (Critical Limb Threatening Ischemia). 
                    Ти маєш знання про:
                    - WIfI класифікацію (Wound, Ischemia, foot Infection)
                    - CRAB оцінку (періпроцедуральна смертність)
                    - 2YLE оцінку (дворічна виживаність)
                    - GLASS класифікацію (анатомічні ураження артерій)
                    - Гемодинамічні показники (КПІ, ППІ)
                    - Методи реваскуляризації
                    
                    Відповідай українською мовою, будь професійним але дружнім. 
                    Надавай точну медичну інформацію, але завжди рекомендуй консультацію з лікарем для конкретних випадків."
                });

                // Історія розмови
                if (request.ConversationHistory != null && request.ConversationHistory.Any())
                {
                    foreach (var msg in request.ConversationHistory)
                    {
                        messages.Add(new
                        {
                            role = msg.Role,
                            content = msg.Content
                        });
                    }
                }

                // Поточне повідомлення
                messages.Add(new
                {
                    role = "user",
                    content = request.Message
                });

                // Запит до OpenAI
                var openAiRequest = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(openAiRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to OpenAI API");

                var response = await openAiClient.PostAsync("v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {StatusCode}, Content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Неправильний API ключ",
                        System.Net.HttpStatusCode.TooManyRequests => "Перевищено ліміт запитів",
                        System.Net.HttpStatusCode.BadRequest => "Некоректний запит до OpenAI",
                        _ => "Помилка при зверненні до OpenAI API"
                    };

                    return StatusCode(500, new ChatResponse
                    {
                        Success = false,
                        Error = errorMessage
                    });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseContent, _jsonOptions);

                var aiMessage = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrEmpty(aiMessage))
                {
                    _logger.LogWarning("Empty response from OpenAI API");
                    return Ok(new ChatResponse
                    {
                        Success = false,
                        Error = "Отримано порожню відповідь від AI"
                    });
                }

                _logger.LogInformation("AI response generated successfully");

                return Ok(new ChatResponse
                {
                    Success = true,
                    Message = aiMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while calling OpenAI API");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Error = "Помилка мережі при зверненні до AI сервісу"
                });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while calling OpenAI API");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Error = "Час очікування відповіді від AI сервісу закінчився"
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Error = "Помилка обробки відповіді від AI сервісу"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AI chat controller");
                return StatusCode(500, new ChatResponse
                {
                    Success = false,
                    Error = "Внутрішня помилка сервера"
                });
            }
        }
    }

    // DTO класи для запитів
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage> ConversationHistory { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
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