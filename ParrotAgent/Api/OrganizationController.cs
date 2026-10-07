using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Models;

namespace ParrotAgent.Api;

[ApiController]
[Route("api/organization")]

public class OrganizationController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;
    
    public OrganizationController(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    [HttpGet("check-domain")]
    public async Task<IActionResult> CheckDomain(string domain  )
    {
        if (string.IsNullOrEmpty(domain))
        {
            return BadRequest("Domain is required");
        }
        Organization? organization = await _context.Organizations.FirstOrDefaultAsync(o => o.Domain == domain);
        if (organization == null)
        {
            return NotFound("Organization not found");
        }
        return Json(new { organization.Id, organization.Name, organization.Domain, organization.Status });
    }
}
