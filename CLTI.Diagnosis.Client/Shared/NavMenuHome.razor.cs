using CLTI.Diagnosis.Client.Algoritm.Services;
using CLTI.Diagnosis.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CLTI.Diagnosis.Client.Shared
{
    public partial class NavMenuHome : IDisposable
    {
        // === Стан меню ===
        private bool showHemodynamic = true;
        private bool showUserMenu = false;

        // === Дані користувача ===
        private string username = "Користувач";
        private string useremail = "user@example.com";
        private UserContexMenu? userContextMenuRef;

        // === Ініціалізація компонента ===
        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChange;
            base.OnInitialized();
        }

        // === Обробка оновлення стану ===
        private void HandleStateChange()
        {
            InvokeAsync(StateHasChanged);
        }

        // === Обробка кнопки "New patient" ===
        private void OnNewPatientClick()
        {
            StateService.Reset();
        }

        // === Перемикання відображення секції гемодинаміки ===
        public void ToggleHemodynamicSection()
        {
            showHemodynamic = !showHemodynamic;
            StateHasChanged();
        }

        // === Відображення / приховання меню користувача ===
        public void ToggleUserMenu(MouseEventArgs e)
        {
            if (!showUserMenu)
            {
                showUserMenu = true;
                userContextMenuRef?.Show(e.ClientX, e.ClientY - 100, username);
            }
            else
            {
                showUserMenu = false;
                userContextMenuRef?.Hide();
            }

            StateHasChanged();
        }

        // === Закриття меню користувача (викликається з дочірнього компонента) ===
        public void CloseUserMenu()
        {
            showUserMenu = false;
            StateHasChanged();
        }

        // === Перехід до налаштувань ===
        public void NavigateToSettings()
        {
            NavigationManager.NavigateTo("/settings");
            showUserMenu = false;
        }

        // === Вихід з системи ===
        public void Logout()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
            showUserMenu = false;
        }

        // === Очистка підписок ===
        public void Dispose()
        {
            StateService.OnChange -= HandleStateChange;
        }
    }
}
