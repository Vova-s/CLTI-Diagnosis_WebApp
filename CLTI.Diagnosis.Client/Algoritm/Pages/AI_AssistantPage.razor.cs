using Microsoft.AspNetCore.Components.Web;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class AI_AssistantPage
    {
        private string MessageText { get; set; } = "";
        private string? LastSentMessage { get; set; }

        private void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(MessageText))
            {
                LastSentMessage = MessageText;
                MessageText = "";
            }
        }

        private void OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                SendMessage();
            }
        }
    }
}
