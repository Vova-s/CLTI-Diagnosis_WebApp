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
            // Підписуємося на подію зміни стану
            StateService.OnChange += HandleStateChange;
            base.OnInitialized();
        }

        private void HandleStateChange()
        {
            // Використовуйте InvokeAsync для забезпечення потоку UI
            InvokeAsync(() => StateHasChanged());
        }

        private List<string> GetHemodynamicItems()
        {
            var items = new List<string> { "КПІ" };

            // Додаємо ППІ тільки якщо він потрібен
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

        // Метод для закриття меню (викликається з UserContexMenu)
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
            // Відписуємось від події при знищенні компонента
            StateService.OnChange -= HandleStateChange;
        }
    }
}