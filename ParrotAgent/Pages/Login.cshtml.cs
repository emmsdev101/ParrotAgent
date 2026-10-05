using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParrotAgent.Database;
using ParrotAgent.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ParrotAgent.Utilities;

namespace ParrotAgent.Pages
{
    public class LoginModel : PageModel
    {

        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LoginView LoginUser { get;set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            if(!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == LoginUser.Email);

            Console.WriteLine($"User: {user?.Username}, Password: {user?.Password}");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(LoginUser.Password);

            if (user == null || !BCrypt.Net.BCrypt.Verify(LoginUser.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            await SessionHandler.LoginUserAsync(HttpContext, user);

            return RedirectToPage("/Account/Dashboard");
        }


        public void OnGet()
        {
        }
    }
}
