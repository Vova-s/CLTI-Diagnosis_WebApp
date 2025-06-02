using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Components
{
    public partial class NotFoundComponent
    {
        private async Task RedirectToServerError()
        {
            var currentPath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var baseUrl = NavigationManager.BaseUri;

            // Use JavaScript to redirect to server-side error page
            await JSRuntime.InvokeVoidAsync("window.location.href", $"{baseUrl}Error?path={Uri.EscapeDataString(currentPath)}&type=404");
        }
    }
}
