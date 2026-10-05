using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using System.Security.Claims;

namespace ParrotAgent.Models
{
    public class PageBaseModel : PageModel
    {
        protected readonly AppDbContext appDbContext;

        protected PageBaseModel(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }


        private User? _currentUser;

        public async Task<User?> GetCurrentUserAsync()
        {
            if(_currentUser != null) return _currentUser;
            
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(int.TryParse(userIdStr, out int userId))
            {
                _currentUser = await appDbContext.Users.FirstOrDefaultAsync(u=>u.Id==userId);
                if(_currentUser != null) return _currentUser;
            }

            var email = User.FindFirstValue(ClaimTypes.Email);

            _currentUser = await appDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            return _currentUser;

        }
        
    }
}
