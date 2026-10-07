using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Models;
using System.Security.Claims;

namespace ParrotAgent.Pages.Account
{
    public class KnowledgeBaseViewModel : PageBaseModel
    {
        private readonly AppDbContext _appDbContext;
        public KnowledgeBaseViewModel(AppDbContext appDbContext) : base(appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public KnowledgeBase UserKnowledgeBase { get; set; } = default!;
        public List<Document> KnowledgeBaseDocuments { get; set; } = new List<Document>();

        public (string CssClass, string IconClass) GetDocumentFileType(string? contentType) => contentType switch
        {
            // PDF
            "application/pdf" => ("pdf", "fa-file-pdf"),

            // Markdown & Text
            "text/markdown" or "text/x-markdown" => ("markdown", "fa-file-code"),
            "text/plain" => ("txt", "fa-file-alt"),
            "text/csv" => ("csv", "fa-file-csv"),

            // Microsoft Office
            "application/msword" or
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ("word", "fa-file-word"),

            "application/vnd.ms-excel" or
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ("excel", "fa-file-excel"),

            "application/vnd.ms-powerpoint" or
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ("powerpoint", "fa-file-powerpoint"),

            // Code & Data
            "application/json" or "text/json" => ("json", "fa-file-code"),
            "application/xml" or "text/xml" => ("xml", "fa-file-code"),
            "text/html" => ("html", "fa-file-code"),

            // Archives
            "application/zip" or "application/x-zip-compressed" or "application/x-rar-compressed" => ("archive", "fa-file-archive"),

            // Images
            "image/png" or "image/jpeg" or "image/webp" or "image/gif" or "image/svg+xml" => ("image", "fa-file-image"),

            // Fallback Default
            _ => ("default", "fa-file")
        };

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userLogged = await GetCurrentUserAsync();
            if (userLogged == null)
            {
                return RedirectToPage("/login");
            }
            UserKnowledgeBase = await _appDbContext.KnowledgeBases.FirstOrDefaultAsync(kb => (kb.UserId == userLogged.Id || kb.OrganizationId == userLogged.OrganizationId) && kb.Id == id);
            KnowledgeBaseDocuments = await _appDbContext.Documents.Where(d => d.KnowledgeBaseId == UserKnowledgeBase.Id && d.OrganizationId == userLogged.OrganizationId).ToListAsync();

            if(UserKnowledgeBase == null)
            {
                return NotFound();
            }

            return Page();
        }

    }
}
