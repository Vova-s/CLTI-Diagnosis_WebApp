using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Components
{
    public partial class Routes
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Check if current URL results in 404
                var currentUrl = NavigationManager.Uri;
                var baseUrl = NavigationManager.BaseUri;
                var relativePath = NavigationManager.ToBaseRelativePath(currentUrl);

                // If we're on a route that doesn't exist, redirect to server error page
                await JSRuntime.InvokeVoidAsync("window.location.replace", $"{baseUrl}Error?path={Uri.EscapeDataString(relativePath)}");
            }
        }
    }
}
