using CLTI.Diagnosis.Client.Infrastructure.Auth;
using CLTI.Diagnosis.Client.Infrastructure.Http;
using CLTI.Diagnosis.Client.Infrastructure.State;
using CLTI.Diagnosis.Client.Features.Diagnosis.Services;
using CLTI.Diagnosis.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CLTI.Diagnosis.Client.Shared
{
    public partial class NavMenuHome : IDisposable
    {
        [Inject] private IUserClientService UserService { get; set; } = default!;

        // === Стан меню ===
        private bool showHemodynamic = true;
        private bool showUserMenu = false;

        // === Дані користувача ===
        private string username = "Користувач";
        private string useremail = "user@example.com";
        private UserInfo? currentUser;
        private UserContexMenu? userContextMenuRef;

        // === Ініціалізація компонента ===
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
                Console.WriteLine("NavMenuHome: Loading user data...");
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

                    Console.WriteLine($"NavMenuHome: User loaded - {username} ({useremail})");
                    StateHasChanged();
                }
                else
                {
                    Console.WriteLine("NavMenuHome: No user data found");
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
            NavigationManager.NavigateTo("/Pages/UserSettings");
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
            UserService.OnUserChanged -= HandleUserChanged;
        }
    }
}