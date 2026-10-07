using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using ParrotAgent.Models;
using ParrotAgent.Database;
using Microsoft.EntityFrameworkCore;

namespace ParrotAgent.Pages
{
    [Authorize]
    public class OrganizationForm
    {
        [Required(ErrorMessage = "Organization name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Use 2–80 characters.")]
        [Display(Name = "Organization name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Domain is required.")]
        [RegularExpression(@"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid domain, like acme.com.")]
        [StringLength(253)]
        [Display(Name = "Domain")]
        public string Domain { get; set; } = string.Empty;
        public int? OrganizationId { get; set; } = null;
        public bool CreateNewOrganization { get; set; } = false;
    }

    public class CreateOrganizationModel : PageBaseModel 
    {
        private readonly AppDbContext _context;

        [BindProperty]
        public OrganizationForm OrganizationForm { get; set; } = new();
        public Organization? ExistingOrganization { get; set; } = null;
        public User? CurrentUser { get; set; } = null;

        public CreateOrganizationModel(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGet()
        {
            if (!await LoadUserContextAsync())
            {
                return RedirectToPage("/Login");
            }

            OrganizationForm.Domain = DomainFromEmail(CurrentUser!.Email);
            OrganizationForm.OrganizationId = ExistingOrganization?.Id;
            OrganizationForm.Name = ExistingOrganization?.Name ?? string.Empty;
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!await LoadUserContextAsync())
            {
                return RedirectToPage("/Register");
            }

            User user = CurrentUser!;

            if (!OrganizationForm.CreateNewOrganization && ExistingOrganization != null)
            {
                user.OrganizationId = ExistingOrganization.Id;
                user.Status = "active";
                user.RoleId = 2;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToPage("/Account/Dashboard");
            }

            OrganizationForm.Domain = NormalizeDomain(OrganizationForm.Domain);
            ModelState.Clear();
            TryValidateModel(OrganizationForm, nameof(OrganizationForm));
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Organization organization = new Organization
            {
                Name = OrganizationForm.Name,
                Domain = NormalizeDomain(OrganizationForm.Domain),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
            user.OrganizationId = organization.Id;
            user.Status = "Active";
            user.RoleId = 2;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return RedirectToPage("/Account/Dashboard");
        }

        private async Task<bool> LoadUserContextAsync()
        {
            User? user = await GetCurrentUserAsync();
            if (user == null)
            {
                return false;
            }

            CurrentUser = user;
            string domain = DomainFromEmail(user.Email);
            if (!string.IsNullOrEmpty(domain))
            {
                ExistingOrganization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Domain == domain);
            }

            return true;
        }

        private static string DomainFromEmail(string email)
        {
            int at = email.LastIndexOf('@');
            if (at < 0 || at >= email.Length - 1)
            {
                return string.Empty;
            }

            return NormalizeDomain(email[(at + 1)..]);
        }

        public static string NormalizeDomain(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var domain = value.Trim().ToLowerInvariant();
            if (domain.Contains('@'))
            {
                domain = domain[(domain.LastIndexOf('@') + 1)..];
            }

            domain = Regex.Replace(domain, @"^[a-z][a-z0-9+.-]*://", "");
            var slash = domain.IndexOf('/');
            if (slash >= 0)
            {
                domain = domain[..slash];
            }

            var colon = domain.IndexOf(':');
            if (colon >= 0)
            {
                domain = domain[..colon];
            }

            if (domain.StartsWith("www.", StringComparison.Ordinal))
            {
                domain = domain[4..];
            }

            return domain.Trim('.');
        }
    }
}
