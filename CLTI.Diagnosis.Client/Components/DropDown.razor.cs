using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class DropDown
    {
        [Parameter] public List<string> Items { get; set; } = new();
        [Parameter] public string? Placeholder { get; set; }
        [Parameter] public string? SelectedItem { get; set; }
        [Parameter] public EventCallback<string> OnSelect { get; set; }
        [Parameter] public string Theme { get; set; } = "light";

        private bool IsOpen { get; set; } = false;
        private string? SelectedItemInternal => SelectedItem;
        private bool IsSelected => !string.IsNullOrEmpty(SelectedItemInternal);

        private string CssClass => Theme switch
        {
            "dark" => "themedark-statedefault",
            _ => "themelight-statedefault"
        };

        private string ListCssClass => Theme switch
        {
            "dark" => "dropdown-dark",
            _ => "dropdown-light"
        };

        private string? StyleString => IsOpen
            ? (Theme == "dark" ? "border: 1px solid #5a5a5a;" : "border: 1px solid #057cff;")
            : null;

        private void ToggleDropdown()
        {
            IsOpen = !IsOpen;
        }

        private async Task SelectItem(string item)
        {
            SelectedItem = item;
            await OnSelect.InvokeAsync(item);
            IsOpen = false;
        }
    }
}
