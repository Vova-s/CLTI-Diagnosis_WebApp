using Microsoft.AspNetCore.Components;

namespace CLTI.Diagnosis.Client.Algoritm.Shared
{
    public partial class NavMenuHome
    {
        private bool showHemodynamic = true;
        private bool showUserMenu = false;
        private string username = "Користувач";
        private string useremail = "user@example.com";

        protected override void OnInitialized()
        {
            // Підписуємося на подію зміни стану
            StateService.OnChange += HandleStateChange;
            base.OnInitialized();
        }

        private void HandleStateChange()
        {
            // Використовуйте InvokeAsync для забезпечення потоку UI
            InvokeAsync(() => StateHasChanged());
        }

        public void ToggleHemodynamicSection()
        {
            showHemodynamic = !showHemodynamic;
            StateHasChanged();
        }

        public void ToggleUserMenu()
        {
            showUserMenu = !showUserMenu;
            StateHasChanged();
        }

        public void NavigateToSettings()
        {
            NavigationManager.NavigateTo("/settings");
            showUserMenu = false;
        }

        public void Logout()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
            showUserMenu = false;
        }

        public void Dispose()
        {
            // Відписуємось від події при знищенні компонента
            StateService.OnChange -= HandleStateChange;
        }


    }
}

