using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using ParrotAgent.Database;
using ParrotAgent.Models;

namespace ParrotAgent.Pages.Account
{
    public class LogoutModel : PageBaseModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogoutModel(IHttpContextAccessor httpContextAccessor, AppDbContext appDbContext) : base(appDbContext)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync();
            return RedirectToPage("/Login");
        }
    }
}