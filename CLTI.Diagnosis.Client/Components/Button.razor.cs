using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class Button : ComponentBase
    {
        [Parameter] public string Label { get; set; } = "Кнопка";
        [Parameter] public string Variant { get; set; } = "active"; // active, disabled, dark-active, dark-disabled
        [Parameter] public EventCallback OnClick { get; set; }
        [Parameter] public bool Disabled { get; set; }

        [Parameter] public string Position { get; set; } = "static";
        [Parameter] public string? Top { get; set; }
        [Parameter] public string? Left { get; set; }
        [Parameter] public string? Right { get; set; }
        [Parameter] public string? Bottom { get; set; }
        [Parameter] public string? Color { get; set; } // наприклад: "#ffcc00" або "red"

        private string CssClass => Variant switch
        {
            "active" => "typeprimary-stateactive-th",
            "disabled" => "typeprimary-statedisabled",
            "dark-active" => "typeprimary-stateactive-th1",
            "dark-disabled" => "typeprimary-statedisabled1",
            _ => "typeprimary-stateactive-th"
        };

        private string StyleString =>
    $"position:{Position};" +
    $"{(Top != null ? $"top:{Top};" : "")}" +
    $"{(Left != null ? $"left:{Left};" : "")}" +
    $"{(Right != null ? $"right:{Right};" : "")}" +
    $"{(Bottom != null ? $"bottom:{Bottom};" : "")}" +
    $"{(Color != null ? $"background-color:{Color};" : "")}";


        private async Task HandleClick()
        {
            if (!Disabled)
                await OnClick.InvokeAsync();
        }
    }
}
