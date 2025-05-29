using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class DropDown
    {
        [Parameter] public List<string> Items { get; set; } = new();
        [Parameter] public EventCallback<string> OnSelect { get; set; }
        [Parameter] public string Placeholder { get; set; } = "Оберіть...";
        [Parameter] public string Theme { get; set; } = "light"; // light | dark
        [Parameter] public string Position { get; set; } = "relative";
        [Parameter] public string? Top { get; set; }
        [Parameter] public string? Left { get; set; }
        [Parameter] public string? Right { get; set; }
        [Parameter] public string? Bottom { get; set; }

        private bool IsOpen { get; set; } = false;
        private string SelectedItemInternal { get; set; } = string.Empty;
        private bool IsSelected => !string.IsNullOrEmpty(SelectedItemInternal);

        private void ToggleDropdown() => IsOpen = !IsOpen;

        private async Task SelectItem(string item)
        {
            SelectedItemInternal = item;
            IsOpen = false;
            await OnSelect.InvokeAsync(item);
        }

        private string CssClass => $"theme{Theme}-state" + (IsOpen ? "focused" : "default");
        private string ListCssClass => Theme == "dark" ? "dropdown-dark" : "dropdown-light";

        private string StyleString =>
            $"position:{Position};" +
            $"{(Top != null ? $"top:{Top};" : "")}" +
            $"{(Left != null ? $"left:{Left};" : "")}" +
            $"{(Right != null ? $"right:{Right};" : "")}" +
            $"{(Bottom != null ? $"bottom:{Bottom};" : "")}";
    }
}
