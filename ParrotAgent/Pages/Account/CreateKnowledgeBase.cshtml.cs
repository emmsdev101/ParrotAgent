using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParrotAgent.Database;
using ParrotAgent.Models;
using System.Security.Claims;

namespace ParrotAgent.Pages.Account
{
    public class CreateKnowledgeBaseModel : PageModel
    {

        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CreateKnowledgeBaseModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public KnowledgeBase NewKnowledgeBase { get; set; } = default!;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            ClaimsPrincipal? UserLogged = _httpContextAccessor.HttpContext?.User;

            string Email = UserLogged?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            var user = _httpContextAccessor.HttpContext?.RequestServices.GetService<AppDbContext>()?.Users.FirstOrDefault(u => u.Email == Email);

            if (user == null)
            {
                return NotFound();
            }

            Console.WriteLine($"User ID: {user.Id}, Email: {user.Email}");

            // all the properties of NewKnowledgeBase 
            Console.WriteLine($"NewKnowledgeBase Properties: Name: {NewKnowledgeBase.Name}, Description: {NewKnowledgeBase.Description}, CreatedAt: {NewKnowledgeBase.CreatedAt}");

            NewKnowledgeBase.UserId = user.Id;

            _context.Add(NewKnowledgeBase);

            await _context.SaveChangesAsync();

            return RedirectToPage("/Account/KnowledgeBases");


        }
    }
}
