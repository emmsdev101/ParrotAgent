using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParrotAgent.Database;
using ParrotAgent.Models;

namespace ParrotAgent.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;

        }

        [BindProperty]
        public User NewUser { get; set; } = default!;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                return Page();
            }

            String hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewUser.Password);

            NewUser.Password = hashedPassword;

            _context.Users.Add(NewUser);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Index");
        }
    }
}
