using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Shared
{
    public partial class NavMenuAlgoritm
    {
        private bool? showHemodynamic = true;
        private bool? showUserMenu = false;
        private string? username = "Користувач";
        private string? useremail = "user@example.com";
        private ElementReference? userMenuAnchorRef;
        private string? menuTopPx = "0px";
        private string? menuLeftPx = "0px";
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
