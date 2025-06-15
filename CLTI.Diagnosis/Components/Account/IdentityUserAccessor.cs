using CLTI.Diagnosis.Data;
using CLTI.Diagnosis.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CLTI.Diagnosis.Components.Account
{
    internal sealed class IdentityUserAccessor
    {
        private readonly ApplicationDbContext _context;
        private readonly IdentityRedirectManager _redirectManager;

        public IdentityUserAccessor(ApplicationDbContext context, IdentityRedirectManager redirectManager)
        {
            _context = context;
            _redirectManager = redirectManager;
        }

        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _redirectManager.RedirectToWithStatus("Account/InvalidUser",
                    "Error: Unable to load user.", context);
                throw new InvalidOperationException("User not found");
            }

            var sysUser = await _context.SysUsers.FindAsync(userId);
            if (sysUser == null)
            {
                _redirectManager.RedirectToWithStatus("Account/InvalidUser",
                    $"Error: Unable to load user with ID '{userId}'.", context);
                throw new InvalidOperationException("User not found");
            }

            return new ApplicationUser
            {
                Id = sysUser.Id.ToString(),
                UserName = sysUser.Email,
                Email = sysUser.Email,
                EmailConfirmed = true
            };
        }
    }
}