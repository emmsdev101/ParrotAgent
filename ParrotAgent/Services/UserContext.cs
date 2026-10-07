using System.Security.Claims;
using ParrotAgent.Database;
namespace ParrotAgent.Services
{

    public interface IUserContext
    {
        string Email { get; }
        string Name { get; }
        string Initials { get; }
        bool IsAuthenticated { get; }
    }

    public class UserContext : IUserContext
    {

        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
        
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
        public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Email)) return "Guest";
                var user = _httpContextAccessor.HttpContext?.RequestServices.GetService<AppDbContext>()?.Users.FirstOrDefault(u => u.Email == Email);
                return user?.FirstName + " " + user?.LastName ?? "Guest";
            }
        }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name) || Name == "Guest") return "PA";
                var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                    : $"{parts[0][0]}".ToUpper();
            }
        }

    }
}
