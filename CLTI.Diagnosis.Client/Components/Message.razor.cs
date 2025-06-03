using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class Message
    {
        [Parameter] public string Text { get; set; } = "Message";
        [Parameter] public string Theme { get; set; } = "light"; // "light" | "dark"
        [Parameter] public string State { get; set; } = "default"; // "success" | "error" | "warning" | "default"
        [Parameter] public string Size { get; set; } = "normal"; // "normal" | "small"
        [Parameter] public string TextAlign { get; set; } = "center";
        [Parameter] public bool IsHtml { get; set; } = false;

        private string GetClass()
        {
            return $"message-box theme-{Theme} state-{State} size-{Size}";
        }

        private string TextClass => Size == "small" ? "message-small" : "message-large";
    }
}
