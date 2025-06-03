using CLTI.Diagnosis.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Shared
{
    public partial class NavMenuAlgoritm : IDisposable
    {
        private bool showUserMenu = false;
        private string username = "Користувач";
        private string useremail = "user@example.com";
        private UserContexMenu? userContextMenuRef;

        protected override void OnInitialized()
        {
            StateService.OnChange += HandleStateChange;
            base.OnInitialized();
        }

        private void HandleStateChange()
        {
            InvokeAsync(() => StateHasChanged());
        }

        public List<string> GetWifiItems()
        {
            var WiFiItems = new List<string> { "Оцінка критерію W" };

            if (StateService.IsWCompleted)
            {
                WiFiItems.Add("Оцінка критерію I");
            }

            if (StateService.IsICompleted)
            {
                WiFiItems.Add("Оцінка критерію fI");
            }

            if (StateService.IsfICompleted)
            {
                WiFiItems.Add("Оцінка результатів");
            }

            return WiFiItems;
        }

        public List<string> GetRiskItems()
        {
            var crabItems = new List<string>();

            if (StateService.IsWiFIResultsCompleted)
            {
                crabItems.Add("Оцінка перипроцедуральної смертності");

            }
            if (StateService.IsCRABCompleted)
            {
                crabItems.Add("Оцінка дворічної виживаності");
            }
            if (StateService.Is2YLECompleted)
            {
                crabItems.Add("Кінцева оцінка ступеня хірургічного ризику");
            }

            return crabItems;
        }

        public List<string> GetGLASSItems()
        {
            var glassItems = new List<string>();

            if (StateService.IsSurgicalRiskCompleted)
            {
                glassItems.Add("Визначення анатомічної стадії аорто-клубової хвороби за GLASS");
            }
            if (StateService.IsGLASSCompleted)
            {
                glassItems.Add("Визначення ступеня ураження стегново-підколінного сегмента");
            }
            if (StateService.IsGLASSFemoroPoplitealCompleted)
            {
                glassItems.Add("Визначення ступеня ураження інфрапоплітеального сегмента");
            }
            if (StateService.IsGLASSInfrapoplitealCompleted)
            {
                glassItems.Add("Остаточне визначення анатомічної стадії інфраінгвінальної хвороби за GLASS");
            }

            return glassItems;
        }

        private List<string> GetHemodynamicItems()
        {
            var items = new List<string> { "КПІ" };

            if (StateService.ShowPpiInSidebar)
            {
                items.Add("ППІ");
            }

            return items;
        }

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

        public void CloseUserMenu()
        {
            showUserMenu = false;
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
            StateService.OnChange -= HandleStateChange;
        }
    }
}