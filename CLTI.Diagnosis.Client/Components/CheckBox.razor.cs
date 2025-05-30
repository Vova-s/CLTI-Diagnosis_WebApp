using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class CheckBox
    {
        [Parameter] public string Label { get; set; } = "Label";
        [Parameter] public bool IsChecked { get; set; }
        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

        private async Task ToggleChecked()
        {
            IsChecked = !IsChecked;
            await IsCheckedChanged.InvokeAsync(IsChecked);
        }
    }

}

