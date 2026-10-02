using ParrotAgent.Models;

namespace ParrotAgent.Services
{

    public interface IContextProvider
    {
        public User User { get; set; }

    }
    public class ContextProvider : IContextProvider
    {
        private readonly HttpContextAccessor _httpContextAccessor;
        public User User { get; set; } = null!;

        public ContextProvider(HttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
    }
}
