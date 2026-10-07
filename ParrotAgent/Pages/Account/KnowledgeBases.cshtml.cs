using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParrotAgent.Database;
using ParrotAgent.Models;
using System.Security.Claims;
namespace ParrotAgent.Pages.Account
{
    public class KnowledgeBasesModel : PageModel
    {

        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public KnowledgeBasesModel(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<KnowledgeBase> UserKnowledgeBases { get; set; } = new List<KnowledgeBase>();

        public void OnGet()
        {
            ClaimsPrincipal userSession = _httpContextAccessor.HttpContext.User;
            string email = userSession.FindFirstValue(ClaimTypes.Email)??String.Empty;
            var UserLogged = _httpContextAccessor.HttpContext.RequestServices.GetService<AppDbContext>().Users.FirstOrDefault(u => u.Email == email);

            UserKnowledgeBases = _context.KnowledgeBases.Where(kb => kb.UserId == UserLogged.Id || kb.OrganizationId == UserLogged.OrganizationId).ToList(); 
        }
    }
}
