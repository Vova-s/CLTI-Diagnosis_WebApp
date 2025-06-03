using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class TextInput : ComponentBase
    {
        // === Параметри інтерфейсу ===
        [Parameter] public string Placeholder { get; set; } = "Placeholder";
        [Parameter] public string Theme { get; set; } = "light"; // "light" | "dark"
        [Parameter] public string State { get; set; } = "default"; // "default" | "focus" | "filled" | "typing"
        [Parameter] public string Type { get; set; } = "text"; // "text" | "password"
        [Parameter] public bool Disabled { get; set; } = false;

        // === Позиціонування (optional) ===
        [Parameter] public string Position { get; set; } = "static";
        [Parameter] public string? Top { get; set; }
        [Parameter] public string? Left { get; set; }
        [Parameter] public string? Right { get; set; }
        [Parameter] public string? Bottom { get; set; }

        // === Значення інпуту ===
        [Parameter] public string Value { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        // === Генерація класів ===
        private string CssClass => $"theme-{Theme}-state{State}";

        private string StyleString =>
            $"position:{Position};" +
            $"{(Top != null ? $"top:{Top};" : "")}" +
            $"{(Left != null ? $"left:{Left};" : "")}" +
            $"{(Right != null ? $"right:{Right};" : "")}" +
            $"{(Bottom != null ? $"bottom:{Bottom};" : "")}";

        // === Обробка зміни значення ===
        private async Task OnValueChanged(ChangeEventArgs e)
        {
            Value = e.Value?.ToString() ?? string.Empty;
            await ValueChanged.InvokeAsync(Value);
        }
    }
}
