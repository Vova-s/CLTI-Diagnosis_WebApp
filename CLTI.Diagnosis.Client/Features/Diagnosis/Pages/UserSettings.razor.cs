using Microsoft.JSInterop;

namespace CLTI.Diagnosis.Client.Features.Diagnosis.Pages
{
    public partial class UserSettings
    {
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _oldPassword = string.Empty;
        private string _password = string.Empty;

        // Властивості з викликом OnFieldChanged
        private string firstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnFieldChanged();
            }
        }

        private string lastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnFieldChanged();
            }
        }

        private string email
        {
            get => _email;
            set
            {
                _email = value;
                OnFieldChanged();
            }
        }

        private string oldPassword
        {
            get => _oldPassword;
            set
            {
                _oldPassword = value;
                OnFieldChanged();
            }
        }

        private string password
        {
            get => _password;
            set
            {
                _password = value;
                OnFieldChanged();
            }
        }

        // Оригінальні значення для порівняння
        private string originalFirstName = string.Empty;
        private string originalLastName = string.Empty;
        private string originalEmail = string.Empty;

        private bool isUpdating = false;
        private bool isDeleting = false;
        private bool hasChanges = false;

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
                    // Встановлюємо поточні значення
                    firstName = userInfo.FirstName ?? string.Empty;
                    lastName = userInfo.LastName ?? string.Empty;
                    email = userInfo.Email ?? string.Empty;

                    // Зберігаємо оригінальні значення
                    originalFirstName = firstName;
                    originalLastName = lastName;
                    originalEmail = email;

                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Помилка завантаження даних користувача";
            }
        }

        private void OnFieldChanged()
        {
            // Перевіряємо, чи є зміни в полях
            hasChanges = firstName != originalFirstName ||
                        lastName != originalLastName ||
                        email != originalEmail ||
                        !string.IsNullOrWhiteSpace(password) ||
                        !string.IsNullOrWhiteSpace(oldPassword);

            StateHasChanged();
        }

        private async Task SaveChanges()
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Ім'я, прізвище та email повинні бути заповнені";
                return;
            }

            if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
            {
                errorMessage = "Пароль повинен містити принаймні 6 символів";
                return;
            }

            // Якщо користувач хоче змінити пароль, перевіряємо чи введений старий пароль
            if (!string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(oldPassword))
            {
                errorMessage = "Для зміни пароля введіть старий пароль";
                return;
            }

            isUpdating = true;
            StateHasChanged();

            try
            {
                bool success = true;

                // Перевіряємо, чи змінилися дані профілю
                bool profileChanged = firstName != originalFirstName ||
                                    lastName != originalLastName ||
                                    email != originalEmail;

                // Оновлюємо профіль тільки якщо є зміни
                if (profileChanged)
                {
                    success = await UserService.UpdateUserAsync(firstName, lastName, email);
                }

                // Змінюємо пароль тільки якщо він був введений і профіль успішно оновлено
                if (success && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(oldPassword))
                {
                    var passwordSuccess = await UserService.ChangePasswordAsync(oldPassword, password);
                    if (!passwordSuccess)
                    {
                        errorMessage = "Помилка зміни пароля. Перевірте правильність старого пароля.";
                        success = false;
                    }
                }

                if (success)
                {
                    string message = "";
                    if (profileChanged && !string.IsNullOrWhiteSpace(password))
                        message = "Профіль та пароль успішно оновлено";
                    else if (profileChanged)
                        message = "Профіль успішно оновлено";
                    else if (!string.IsNullOrWhiteSpace(password))
                        message = "Пароль успішно змінено";

                    successMessage = message;

                    // Оновлюємо оригінальні значення
                    originalFirstName = firstName;
                    originalLastName = lastName;
                    originalEmail = email;
                    oldPassword = string.Empty; // Очищуємо поля паролів
                    password = string.Empty;

                    hasChanges = false;
                }
                else if (string.IsNullOrEmpty(errorMessage)) // Якщо помилка ще не встановлена
                {
                    errorMessage = "Помилка збереження налаштувань. Можливо, email вже використовується.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Виникла помилка при збереженні налаштувань";
            }
            finally
            {
                isUpdating = false;
                StateHasChanged();
            }
        }

        private async Task DeleteAccount()
        {
            ClearMessages();

            var confirmed = await JS.InvokeAsync<bool>("confirm",
                "Ви дійсно хочете видалити свій акаунт?\n\nУвага: Ця дія незворотна! Всі ваші дані будуть втрачені назавжди.\n\nПродовжити?");

            if (!confirmed)
                return;

            // Додаткове підтвердження
            var doubleConfirmed = await JS.InvokeAsync<bool>("confirm",
                "Останнє підтвердження!\n\nВи абсолютно впевнені, що хочете видалити акаунт?\nВідновити його буде неможливо!");

            if (!doubleConfirmed)
                return;

            isDeleting = true;
            StateHasChanged();

            try
            {
                var success = await UserService.DeleteUserAsync();
                if (success)
                {
                    await JS.InvokeVoidAsync("alert", "Акаунт успішно видалено. До побачення!");
                    NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
                }
                else
                {
                    errorMessage = "Помилка видалення акаунту";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Виникла помилка при видаленні акаунту";
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