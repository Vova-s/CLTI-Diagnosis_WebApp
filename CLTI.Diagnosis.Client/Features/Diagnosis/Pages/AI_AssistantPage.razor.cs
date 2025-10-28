using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class AI_AssistantPage
    {
        [Inject] private AiChatClient AiChatClient { get; set; } = default!;
        [Inject] private ILogger<AI_AssistantPage> Logger { get; set; } = default!;

        private string MessageText { get; set; } = "";
        private List<MessageItem> Messages { get; set; } = new();
        private bool IsLoading { get; set; } = false;

        protected override void OnInitialized()
        {
            // Додаємо початкові повідомлення
            Messages.Add(new MessageItem { Text = "Привіт! Я ваш AI асистент з діагностики CLTI.\n Як я можу вам допомогти сьогодні?", Role = "ai" });
            //Messages.Add(new MessageItem { Text = "Як я можу вам допомогти сьогодні?", Role = "ai" });
        }

        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || IsLoading)
                return;

            var userMessage = MessageText.Trim();
            MessageText = "";

            // Додаємо повідомлення користувача
            Messages.Add(new MessageItem { Text = userMessage, Role = "user" });
            StateHasChanged();

            // Показуємо індикатор завантаження
            IsLoading = true;
            Messages.Add(new MessageItem { Text = "АІ друкує...", Role = "ai", IsTyping = true });
            StateHasChanged();

            try
            {
                Logger.LogInformation("Sending message to AI: {Message}", userMessage);

                // Підготовуємо історію розмови для контексту (останні 10 повідомлень)
                var conversationHistory = Messages
                    .Where(m => !m.IsTyping && m.Role != "system")
                    .TakeLast(10) // Беремо тільки останні 10 повідомлень для контексту
                    .Select(m => new ChatMessage
                    {
                        Role = m.Role == "ai" ? "assistant" : "user",
                        Content = m.Text
                    })
                    .ToList();

                // Видаляємо поточне користувацьке повідомлення з історії (воно буде додано в AiChatClient)
                if (conversationHistory.Any() && conversationHistory.Last().Role == "user")
                {
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }

                // Отримуємо відповідь від AI
                var aiResponse = await AiChatClient.SendMessageAsync(userMessage, conversationHistory);

                // Видаляємо індикатор завантаження
                Messages.RemoveAt(Messages.Count - 1);

                if (!string.IsNullOrEmpty(aiResponse))
                {
                    // Додаємо відповідь AI
                    Messages.Add(new MessageItem { Text = aiResponse, Role = "ai" });
                    Logger.LogInformation("AI response received successfully");
                }
                else
                {
                    Messages.Add(new MessageItem
                    {
                        Text = "Вибачте, не вдалося отримати відповідь від AI.",
                        Role = "ai"
                    });
                    Logger.LogWarning("Empty AI response received");
                }
            }
            catch (Exception ex)
            {
                // Видаляємо індикатор завантаження
                if (Messages.Any() && Messages.Last().IsTyping)
                {
                    Messages.RemoveAt(Messages.Count - 1);
                }

                // Додаємо повідомлення про помилку
                Messages.Add(new MessageItem
                {
                    Text = "Вибачте, виникла помилка при зверненні до AI асистента. Спробуйте пізніше.",
                    Role = "ai"
                });

                Logger.LogError(ex, "Error sending message to AI");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey && !IsLoading)
            {
                await SendMessage();
            }
        }

        public class MessageItem
        {
            public string Text { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty; // "user" or "ai"
            public bool IsTyping { get; set; } = false;
        }
    }
}