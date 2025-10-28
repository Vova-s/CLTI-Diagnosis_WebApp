using System.Net.Http.Json;
using CLTI.Diagnosis.Client.Infrastructure.Auth;
using System.Text.Json;

namespace CLTI.Diagnosis.Client.Infrastructure.Http
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
                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    return "Будь ласка, введіть повідомлення.";
                }

                // Підготовуємо повідомлення для відправки на сервер
                var messages = new List<ChatMessage>();

                // Додаємо системне повідомлення
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
                if (conversationHistory != null && conversationHistory.Any())
                {
                    messages.AddRange(conversationHistory);
                }

                // Додаємо поточне повідомлення
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = userMessage
                });

                // Готуємо запит для відправки на сервер
                var chatRequest = new ChatRequest
                {
                    Message = userMessage,
                    ConversationHistory = conversationHistory ?? new List<ChatMessage>()
                };

                _logger.LogInformation("Sending chat request to server API");

                // Відправляємо запит на наш сервер
                var response = await _httpClient.PostAsJsonAsync("/api/aichat/send", chatRequest, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseContent, _jsonOptions);

                    if (chatResponse?.Success == true && !string.IsNullOrEmpty(chatResponse.Message))
                    {
                        _logger.LogInformation("AI response received successfully");
                        return chatResponse.Message;
                    }
                    else
                    {
                        var errorMsg = chatResponse?.Error ?? "Невідома помилка сервера";
                        _logger.LogWarning("Server returned error: {Error}", errorMsg);
                        return $"Помилка: {errorMsg}";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Server API Error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Помилка авторизації. Зверніться до адміністратора.",
                        System.Net.HttpStatusCode.TooManyRequests => "Занадто багато запитів. Спробуйте через хвилину.",
                        System.Net.HttpStatusCode.BadRequest => "Некоректний запит.",
                        System.Net.HttpStatusCode.InternalServerError => "Внутрішня помилка сервера. Спробуйте пізніше.",
                        _ => "Помилка при зверненні до AI сервісу. Спробуйте пізніше."
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while calling server API");
                return "Помилка мережі. Перевірте з'єднання з інтернетом.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout while calling server API");
                return "Час очікування відповіді закінчився. Спробуйте ще раз.";
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
                return "Помилка обробки відповіді сервера.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AiChatClient");
                return "Вибачте, виникла технічна помилка. Спробуйте пізніше.";
            }
        }
    }

    // DTO класи для комунікації з сервером
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage> ConversationHistory { get; set; } = new();
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Error { get; set; }
    }

    // DTO класи для повідомлень
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}