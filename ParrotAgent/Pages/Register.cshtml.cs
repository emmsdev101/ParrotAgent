using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Models;
using ParrotAgent.Utilities;
namespace ParrotAgent.Pages
{
    public class RegistrationForm
    {
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*[0-9]).{8,128}$",
            ErrorMessage = "Use 8–128 characters with at least one letter and one number.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;

        }

        [BindProperty]
        public RegistrationForm UserFormData { get; set; } = new RegistrationForm();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                return Page();
            }

            String hashedPassword = BCrypt.Net.BCrypt.HashPassword(UserFormData.Password);

            Role? userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                ModelState.AddModelError("Role", "User role not found");
                return Page();
            }

             User NewUser = new User
            {
                Email = UserFormData.Email,
                FirstName = UserFormData.FirstName,
                LastName = UserFormData.LastName,
                Password = hashedPassword,
                RoleId = 2,
                Status = "Pending",
                Role = userRole,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(NewUser);

            await _context.SaveChangesAsync();

            await SessionHandler.LoginUserAsync(HttpContext, NewUser);

            return RedirectToPage("/CreateOrganization");
        }
    }
}
