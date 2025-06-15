using CLTI.Diagnosis.Client.Components;
using CLTI.Diagnosis.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Shared
{
    public partial class NavMenuAlgoritm : IDisposable
    {
        [Inject] private IUserClientService UserService { get; set; } = default!;

        // === Стан UI ===
        private bool showUserMenu = false;
        private string username = "Користувач";
        private string useremail = "user@example.com";
        private UserInfo? currentUser;
        private UserContexMenu? userContextMenuRef;

        // === Життєвий цикл ===
        protected override async Task OnInitializedAsync()
        {
            StateService.OnChange += HandleStateChange;
            UserService.OnUserChanged += HandleUserChanged;

            // Завантажуємо дані користувача
            await LoadUserData();

            base.OnInitialized();
        }

        // === Завантаження даних користувача ===
        private async Task LoadUserData()
        {
            try
            {
                currentUser = await UserService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    username = currentUser.FullName ?? $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                    useremail = currentUser.Email ?? "user@example.com";

                    // Якщо ім'я порожнє, використовуємо email
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        username = currentUser.Email ?? "Користувач";
                    }

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                // Логуємо помилку, але не блокуємо UI
                Console.WriteLine($"Error loading user data: {ex.Message}");
            }
        }

        // === Обробка зміни користувача ===
        private void HandleUserChanged(UserInfo? user)
        {
            currentUser = user;
            if (user != null)
            {
                username = user.FullName ?? $"{user.FirstName} {user.LastName}".Trim();
                useremail = user.Email ?? "user@example.com";

                if (string.IsNullOrWhiteSpace(username))
                {
                    username = user.Email ?? "Користувач";
                }
            }
            else
            {
                username = "Користувач";
                useremail = "user@example.com";
            }

            InvokeAsync(StateHasChanged);
        }

        private void HandleStateChange()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            StateService.OnChange -= HandleStateChange;
            UserService.OnUserChanged -= HandleUserChanged;
        }

        // === Меню користувача ===
        public void ToggleUserMenu(MouseEventArgs e)
        {
            showUserMenu = !showUserMenu;

            if (showUserMenu)
                userContextMenuRef?.Show(e.ClientX, e.ClientY - 100, username);
            else
                userContextMenuRef?.Hide();

            StateHasChanged();
        }

        public void CloseUserMenu()
        {
            showUserMenu = false;
            StateHasChanged();
        }

        public void NavigateToSettings()
        {
            NavigationManager.NavigateTo("/Pages/UserSettings");
            showUserMenu = false;
        }

        public void Logout()
        {
            NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
            showUserMenu = false;
        }

        // === Дані меню: WIfI ===
        public List<string> GetWifiItems()
        {
            var WiFiItems = new List<string> { "Оцінка критерію W" };

            if (StateService.IsWCompleted)
                WiFiItems.Add("Оцінка критерію I");

            if (StateService.IsICompleted)
                WiFiItems.Add("Оцінка критерію fI");

            if (StateService.IsfICompleted)
                WiFiItems.Add("Оцінка результатів");

            return WiFiItems;
        }

        // === Дані меню: CRAB / 2YLE ===
        public List<string> GetRiskItems()
        {
            var crabItems = new List<string>();

            if (StateService.IsWiFIResultsCompleted)
                crabItems.Add("Оцінка перипроцедуральної смертності");

            if (StateService.IsCRABCompleted)
                crabItems.Add("Оцінка дворічної виживаності");

            if (StateService.Is2YLECompleted)
                crabItems.Add("Кінцева оцінка ступеня хірургічного ризику");

            return crabItems;
        }

        // === Дані меню: GLASS ===
        public List<string> GetGLASSItems()
        {
            var glassItems = new List<string>();

            if (StateService.IsSurgicalRiskCompleted)
                glassItems.Add("Визначення анатомічної стадії аорто-клубової хвороби за GLASS");

            if (StateService.IsGLASSCompleted)
                glassItems.Add("Визначення ступеня ураження стегново-підколінного сегмента");

            if (StateService.IsGLASSFemoroPoplitealCompleted)
                glassItems.Add("Визначення ступеня ураження інфрапоплітеального сегмента");

            if (StateService.IsGLASSInfrapoplitealCompleted)
                glassItems.Add("Остаточне визначення анатомічної стадії інфраінгвінальної хвороби за GLASS");

            if (StateService.IsGLASSFinalCompleted)
                glassItems.Add("Встановлення дескриптора підкісточкової (стопної) хвороби та формулювання діагнозу пацієнта");

            return glassItems;
        }

        // === Дані меню: Хірургічна тактика ===
        public List<string> GetSurgicalTacticsItems()
        {
            var tacticsItems = new List<string>();

            if (StateService.IsSubmalleolarDiseaseCompleted)
                tacticsItems.Add("Оцінка показань до реваскуляризації");

            if (StateService.IsRevascularizationAssessmentCompleted)
                tacticsItems.Add("Вибір оптимального методу реваскуляризації");

            return tacticsItems;
        }

        // === Дані меню: Гемодинаміка ===
        private List<string> GetHemodynamicItems()
        {
            var items = new List<string> { "КПІ" };

            if (StateService.ShowPpiInSidebar)
                items.Add("ППІ");

            return items;
        }
    }
}