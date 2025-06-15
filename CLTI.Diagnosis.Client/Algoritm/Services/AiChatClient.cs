using System.Net.Http.Json;
using System.Text.Json;

namespace CLTI.Diagnosis.Client.Services
{
    public class AiChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly IClientApiKeyService _apiKeyService;
        private readonly ILogger<AiChatClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AiChatClient(HttpClient httpClient, IClientApiKeyService apiKeyService, ILogger<AiChatClient> logger)
        {
            _httpClient = httpClient;
            _apiKeyService = apiKeyService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<string> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory = null)
        {
            try
            {
                // Отримуємо API ключ через клієнтський сервіс
                var apiKey = await _apiKeyService.GetOpenAiApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("OpenAI API key not available");
                    return "Вибачте, AI асистент тимчасово недоступний. Зверніться до адміністратора.";
                }

                // Створюємо окремий HttpClient для OpenAI API
                using var openAiClient = new HttpClient();
                openAiClient.BaseAddress = new Uri("https://api.openai.com/");
                openAiClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                openAiClient.DefaultRequestHeaders.Add("User-Agent", "CLTI-Diagnosis/1.0");
                openAiClient.Timeout = TimeSpan.FromSeconds(30);

                var messages = new List<ChatMessage>();

                // Системне повідомлення з контекстом про CLTI
                messages.Add(new ChatMessage
                {
                    Role = "system",
                    Content = @"Ти - AI асистент для медичної системи діагностики CLTI (Critical Limb Threatening Ischemia). 
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

                // Додаємо історію розмови
                if (conversationHistory != null)
                {
                    messages.AddRange(conversationHistory);
                }

                // Додаємо поточне повідомлення
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = userMessage
                });

                var request = new
                {
                    model = "gpt-3.5-turbo",
                    messages = messages,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var response = await openAiClient.PostAsJsonAsync("v1/chat/completions", request, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);

                    var aiResponse = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
                    if (!string.IsNullOrEmpty(aiResponse))
                    {
                        _logger.LogInformation("AI response generated successfully for user message");
                        return aiResponse;
                    }

                    _logger.LogWarning("Empty response from OpenAI API");
                    return "Вибачте, не вдалося отримати відповідь.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API Error: {StatusCode} - {ErrorContent}",
                        response.StatusCode, errorContent);

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Помилка авторизації API. Зверніться до адміністратора.",
                        System.Net.HttpStatusCode.TooManyRequests => "Занадто багато запитів. Спробуйте через хвилину.",
                        System.Net.HttpStatusCode.BadRequest => "Некоректний запит до AI сервісу.",
                        _ => "Вибачте, виникла помилка при зверненні до AI асистента. Спробуйте пізніше."
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while calling OpenAI API");
                return "Помилка мережі. Перевірте з'єднання з інтернетом.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while calling OpenAI API");
                return "Час очікування відповіді закінчився. Спробуйте ще раз.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AiChatClient");
                return "Вибачте, виникла технічна помилка. Спробуйте пізніше.";
            }
        }
    }

    // DTO класи для OpenAI API
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatCompletionResponse
    {
        public List<ChatChoice> Choices { get; set; } = new();
    }

    public class ChatChoice
    {
        public ChatMessage Message { get; set; } = new();
        public string FinishReason { get; set; } = string.Empty;
        public int Index { get; set; }
    }
}