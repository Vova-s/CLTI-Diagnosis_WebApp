using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class AI_Message
    {
        [Parameter] public string Text { get; set; } = string.Empty;
        [Parameter] public string Role { get; set; } = "ai"; // "ai", "user", "error"

        private string GetBubbleClass() => Role switch
        {
            "user" => "chat-bubble user",
            "error" => "chat-bubble error",
            _ => "chat-bubble ai"
        };
    }
}