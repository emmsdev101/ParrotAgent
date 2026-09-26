using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ParrotAgent.Models;

namespace ParrotAgent.Pages.Account
{

    [Authorize]
    public class DashboardModel : PageModel
    {


        public void OnGet()
        {
        }
    }
}
