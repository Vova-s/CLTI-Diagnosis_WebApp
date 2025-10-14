using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Components
{
    public partial class Routes
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Removed problematic redirect logic that interferes with authentication
            await Task.CompletedTask;
        }
    }
}
