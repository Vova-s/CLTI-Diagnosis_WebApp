using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class UserContexMenu
    {
        private bool Hidden { get; set; } = true;
        private string menuTopPx = "0px";
        private string menuLeftPx = "0px";

        private string targetName;

        [Parameter]
        public EventCallback OnClose { get; set; }

        public void Show(double x, double y, string name)
        {
            menuLeftPx = $"{x}px";
            menuTopPx = $"{y}px";
            targetName = name;
            Hidden = false;
            StateHasChanged();
        }

        public async void Hide()
        {
            Hidden = true;
            await OnClose.InvokeAsync();
            StateHasChanged();
        }

        private void OnActionClick(string action)
        {
            Console.WriteLine($"Обрана дія: {action}");
            Hide();
        }

        private async Task OnEditProfile()
        {
            NavigationManager.NavigateTo("/Pages/UserSettings", forceLoad: true);
            Hide();
        }

        private async Task OnExit()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
            Hide();
        }
    }
}