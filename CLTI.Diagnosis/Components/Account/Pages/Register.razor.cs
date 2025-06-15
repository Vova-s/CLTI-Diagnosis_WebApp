using CLTI.Diagnosis.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CLTI.Diagnosis.Components.Account.Pages
{
    public partial class Register
    {
        private string? identityErrors;

        [SupplyParameterFromForm]
        private InputModel Input { get; set; } = new();

        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }

        private string? Message => identityErrors;

        public async Task RegisterUser(EditContext editContext)
        {
            try
            {
                // Check if user already exists
                var existingUser = await DbContext.SysUsers
                    .FirstOrDefaultAsync(u => u.Email == Input.Email);

                if (existingUser != null)
                {
                    identityErrors = "Error: User with this email already exists.";
                    return;
                }

                // Get default status (assuming Active = 1)
                var activeStatus = await DbContext.SysEnumItems
                    .FirstOrDefaultAsync(e => e.Name == "Active" && e.SysEnum.Name == "UserStatus");

                if (activeStatus == null)
                {
                    // Create default status if it doesn't exist
                    var statusEnum = await DbContext.SysEnums
                        .FirstOrDefaultAsync(e => e.Name == "UserStatus");

                    if (statusEnum == null)
                    {
                        statusEnum = new SysEnum
                        {
                            Name = "UserStatus",
                            OrderingType = "Manual",
                            OrderingTypeEditor = "Manual",
                            Guid = Guid.NewGuid()
                        };
                        DbContext.SysEnums.Add(statusEnum);
                        await DbContext.SaveChangesAsync();
                    }

                    activeStatus = new SysEnumItem
                    {
                        SysEnumId = statusEnum.Id,
                        Name = "Active",
                        Value = "1",
                        OrderIndex = 1,
                        Guid = Guid.NewGuid()
                    };
                    DbContext.SysEnumItems.Add(activeStatus);
                    await DbContext.SaveChangesAsync();
                }

                // Hash the password
                var hashedPassword = HashPassword(Input.Password);

                // Create new user
                var newUser = new SysUser
                {
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Email = Input.Email,
                    Password = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    StatusEnumItemId = activeStatus.Id,
                    Guid = Guid.NewGuid()
                };

                DbContext.SysUsers.Add(newUser);
                await DbContext.SaveChangesAsync();

                Logger.LogInformation("User {Email} created a new account with password.", Input.Email);

                // Automatically sign in the user
                var claims = new List<System.Security.Claims.Claim>
            {
                new(System.Security.Claims.ClaimTypes.NameIdentifier, newUser.Id.ToString()),
                new(System.Security.Claims.ClaimTypes.Name, newUser.Email),
                new(System.Security.Claims.ClaimTypes.Email, newUser.Email)
            };

                var identity = new System.Security.Claims.ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

                // Use NavigationManager instead of RedirectManager
                var redirectUrl = string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl;
                NavigationManager.NavigateTo(redirectUrl, forceLoad: true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating user {Email}", Input.Email);
                identityErrors = "Error: An error occurred during registration.";
            }
        }

        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

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

