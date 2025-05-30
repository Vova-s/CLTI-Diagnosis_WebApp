using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class SideBarDropDown
    {
        [Parameter] public string Placeholder { get; set; } = "Оберіть...";
        [Parameter] public List<string> Items { get; set; } = new();
        [Parameter] public EventCallback<Dictionary<string, bool>> OnChanged { get; set; }

        private bool IsOpen { get; set; } = false;
        private Dictionary<string, bool> SelectedItems { get; set; } = new();

        private void ToggleOpen() => IsOpen = !IsOpen;

        private async Task ToggleItem(string item)
        {
            if (!SelectedItems.ContainsKey(item))
                SelectedItems[item] = true;
            else
                SelectedItems[item] = !SelectedItems[item];

            await OnChanged.InvokeAsync(SelectedItems);
        }
    }
}
