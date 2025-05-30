using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class RadioButton
    {
        [Parameter] public string Label { get; set; } = "Label";
        [Parameter] public bool IsSelected { get; set; }
        [Parameter] public EventCallback<bool> IsSelectedChanged { get; set; }

        private async Task Select()
        {
            if (!IsSelected)
            {
                IsSelected = true;
                await IsSelectedChanged.InvokeAsync(IsSelected);
            }
        }
    }

}

