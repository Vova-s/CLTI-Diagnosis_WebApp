using CLTI.Diagnosis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class AI_AssistantPage
    {
        [Inject] private AiChatClient AiChatClient { get; set; } = default!;

        private string MessageText { get; set; } = "";
        private List<MessageItem> Messages { get; set; } = new();
        private bool IsLoading { get; set; } = false;

        protected override void OnInitialized()
        {
            // Додаємо початкові повідомлення
            Messages.Add(new MessageItem { Text = "Привіт", Role = "user" });
            Messages.Add(new MessageItem { Text = "Привіт! Як я сьогодні можу тобі допомогти?", Role = "ai" });
            Messages.Add(new MessageItem { Text = "Що таке WIfI?", Role = "user" });
            Messages.Add(new MessageItem
            {
                Text = "WIfI (Wound, Ischemia, and foot Infection) — це клінічна класифікаційна система, яка використовується судинними хірургами для оцінки ризику ампутації кінцівки та потреби в реваскуляризації у пацієнтів із хронічною критичною ішемією кінцівок (CLTI).",
                Role = "ai"
            });
        }

        private async Task SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(MessageText) && !IsLoading)
            {
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
                    // Підготовуємо історію розмови для контексту
                    var conversationHistory = Messages
                        .Where(m => !m.IsTyping)
                        .Skip(4) // Пропускаємо початкові демо-повідомлення
                        .Select(m => new ChatMessage
                        {
                            Role = m.Role == "ai" ? "assistant" : "user",
                            Content = m.Text
                        })
                        .ToList();

                    // Отримуємо відповідь від AI
                    var aiResponse = await AiChatClient.SendMessageAsync(userMessage, conversationHistory);

                    // Видаляємо індикатор завантаження
                    Messages.RemoveAt(Messages.Count - 1);

                    // Додаємо відповідь AI
                    Messages.Add(new MessageItem { Text = aiResponse, Role = "ai" });
                }
                catch (Exception ex)
                {
                    // Видаляємо індикатор завантаження
                    Messages.RemoveAt(Messages.Count - 1);

                    // Додаємо повідомлення про помилку
                    Messages.Add(new MessageItem
                    {
                        Text = "Вибачте, виникла помилка при зверненні до AI асистента. Спробуйте пізніше.",
                        Role = "ai"
                    });

                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                    StateHasChanged();
                }
            }
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
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