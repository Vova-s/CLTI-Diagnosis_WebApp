namespace CLTI.Diagnosis.Models.DTOs
{
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
