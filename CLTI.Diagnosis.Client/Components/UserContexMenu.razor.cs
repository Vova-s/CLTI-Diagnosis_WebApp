using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Components
{
    public partial class UserContexMenu
    {
        // === Внутрішній стан ===
        private bool Hidden { get; set; } = true;
        private string menuTopPx = "0px";
        private string menuLeftPx = "0px";
        private string? targetName;

        // === Подія закриття меню (із зовнішнього компонента) ===
        [Parameter]
        public EventCallback OnClose { get; set; }

        // === Відображення меню з координатами ===
        public void Show(double x, double y, string name)
        {
            menuLeftPx = $"{x}px";
            menuTopPx = $"{y}px";
            targetName = name;
            Hidden = false;
            StateHasChanged();
        }

        // === Приховування меню ===
        public async void Hide()
        {
            Hidden = true;
            await OnClose.InvokeAsync();
            StateHasChanged();
        }

        // === Дії меню ===
        private void OnEditProfile()
        {
            NavigationManager.NavigateTo("/Pages/UserSettings", forceLoad: true);
            Hide();
        }

        private void OnExit()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
            Hide();
        }
    }
}
