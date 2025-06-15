using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Algoritm.Pages
{
    public partial class UserSettings
    {
        private string firstName = string.Empty;
        private string lastName = string.Empty;
        private string email = string.Empty;
        private string oldPassword = string.Empty;
        private string newPassword = string.Empty;
        private string confirmPassword = string.Empty;
        private string deletePassword = string.Empty;

        private bool isUpdating = false;
        private bool isChangingPassword = false;
        private bool isDeleting = false;

        private string successMessage = string.Empty;
        private string errorMessage = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadCurrentUser();
        }

        private async Task LoadCurrentUser()
        {
            try
            {
                var userInfo = await UserService.GetCurrentUserAsync();
                if (userInfo != null)
                {
                    firstName = userInfo.FirstName ?? string.Empty;
                    lastName = userInfo.LastName ?? string.Empty;
                    email = userInfo.Email ?? string.Empty;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Помилка завантаження даних користувача";
            }
        }

        private async Task UpdateProfile()
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Всі поля повинні бути заповнені";
                return;
            }

            isUpdating = true;
            StateHasChanged();

            try
            {
                var success = await UserService.UpdateUserAsync(firstName, lastName, email);
                if (success)
                {
                    successMessage = "Профіль успішно оновлено";
                }
                else
                {
                    errorMessage = "Помилка оновлення профілю. Можливо, email вже використовується.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Виникла помилка при оновленні профілю";
            }
            finally
            {
                isUpdating = false;
                StateHasChanged();
            }
        }

        private async Task ChangePassword()
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                errorMessage = "Введіть старий та новий пароль";
                return;
            }

            if (newPassword.Length < 6)
            {
                errorMessage = "Новий пароль повинен містити принаймні 6 символів";
                return;
            }

            if (newPassword != confirmPassword)
            {
                errorMessage = "Нові паролі не співпадають";
                return;
            }

            isChangingPassword = true;
            StateHasChanged();

            try
            {
                var success = await UserService.ChangePasswordAsync(oldPassword, newPassword);
                if (success)
                {
                    successMessage = "Пароль успішно змінено";
                    oldPassword = string.Empty;
                    newPassword = string.Empty;
                    confirmPassword = string.Empty;
                }
                else
                {
                    errorMessage = "Помилка зміни пароля. Перевірте правильність старого пароля.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Виникла помилка при зміні пароля";
            }
            finally
            {
                isChangingPassword = false;
                StateHasChanged();
            }
        }

        private async Task DeleteUser()
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(deletePassword))
            {
                errorMessage = "Введіть пароль для підтвердження видалення";
                return;
            }

            var confirmed = await JS.InvokeAsync<bool>("confirm",
                "Ви впевнені, що хочете видалити свій акаунт? Цю дію неможливо буде скасувати!");

            if (!confirmed)
                return;

            isDeleting = true;
            StateHasChanged();

            try
            {
                // Спершу перевіряємо пароль через спробу його зміни на той самий
                var passwordCorrect = await UserService.ChangePasswordAsync(deletePassword, deletePassword);
                if (!passwordCorrect)
                {
                    errorMessage = "Неправильний пароль";
                    return;
                }

                var success = await UserService.DeleteUserAsync();
                if (success)
                {
                    // Перенаправляємо на головну сторінку після видалення
                    NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
                }
                else
                {
                    errorMessage = "Помилка видалення користувача";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Виникла помилка при видаленні користувача";
            }
            finally
            {
                isDeleting = false;
                StateHasChanged();
            }
        }

        private void ClearMessages()
        {
            successMessage = string.Empty;
            errorMessage = string.Empty;
        }
    }
}
