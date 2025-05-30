using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class AI_Message
    {
        [Parameter] public string Text { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = "ai"; // "ai" або "user"

        private string GetBubbleClass() => Role == "user" ? "chat-bubble user" : "chat-bubble ai";
    }
}