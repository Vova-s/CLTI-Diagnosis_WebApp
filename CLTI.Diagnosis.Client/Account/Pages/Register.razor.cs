using CLTI.Diagnosis.Core.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CLTI.Diagnosis.Client.Account.Pages
{
    public partial class Register
    {
        private string? Message;
        private string? successMessage;
        private bool isLoading = false;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }

        public async Task RegisterUser()
        {
            if (isLoading)
                return;

            try
            {
                isLoading = true;
                Message = null;
                successMessage = null;
                StateHasChanged();

                Logger.LogInformation("Attempting JWT registration for user: {Email}", Input.Email);

                // Використовуємо AuthApiService для JWT реєстрації
                var result = await AuthApi.RegisterAsync(Input.Email, Input.Password, Input.FirstName ?? "", Input.LastName);

                if (result.Success)
                {
                    Logger.LogInformation("JWT Registration successful for user: {Email}", Input.Email);

                    successMessage = "Registration successful! You can now log in.";

                    // Очищаємо форму
                    Input = new InputModel();

                    // Через кілька секунд перенаправляємо на логін
                    await Task.Delay(2000);
                    NavigationManager.NavigateTo("/Account/Login", forceLoad: true);
                }
                else
                {
                    Message = result.Message ?? "Registration failed";
                    Logger.LogWarning("JWT Registration failed for user {Email}: {Error}", Input.Email, Message);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "Network error during JWT registration for user {Email}", Input.Email);
                Message = "Network error. Please check your connection and try again.";
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogError(ex, "Timeout during JWT registration for user {Email}", Input.Email);
                Message = "Request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during JWT registration for user {Email}", Input.Email);
                Message = "An unexpected error occurred. Please try again.";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private sealed class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Display(Name = "First Name")]
            public string? FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = "";

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }
    }
}
