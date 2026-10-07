using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Api;
using ParrotAgent.Database;
using ParrotAgent.Models;
using ParrotAgent.Utilities;
using System.Net;
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
        public KnowledgeBaseEmbed? EmbedSettings { get; set; }
        public bool ShowEmbedModal { get; set; }
        public bool EmbedSaved { get; set; }
        public string? EmbedError { get; set; }

        [BindProperty]
        public string EmbedAllowedOrigins { get; set; } = string.Empty;

        [BindProperty]
        public bool EmbedEnabled { get; set; }

        public string? EmbedSnippet => EmbedSettings == null || UserKnowledgeBase == null
            ? null
            : $"<script src=\"{Request.Scheme}://{Request.Host}/js/kb-chat.js\" data-parrot-token=\"{EmbedSettings.Token}\" data-parrot-name=\"{WebUtility.HtmlEncode(UserKnowledgeBase.Name)}\" defer></script>";

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
            var denied = await LoadAsync(id);
            if (denied != null)
            {
                return denied;
            }

            EmbedAllowedOrigins = (EmbedSettings?.AllowedOrigins ?? string.Empty).Replace("\n", Environment.NewLine);
            EmbedEnabled = EmbedSettings?.IsEnabled ?? false;
            ShowEmbedModal = Request.Query["embed"] == "open";
            EmbedSaved = Request.Query["saved"] == "1";
            return Page();
        }

        public async Task<IActionResult> OnPostSaveEmbedAsync(int id, bool regenerate)
        {
            var denied = await LoadAsync(id);
            if (denied != null)
            {
                return denied;
            }

            if (!EmbedOriginRules.TryParseList(EmbedAllowedOrigins, out var origins, out var error))
            {
                EmbedError = error;
                ShowEmbedModal = true;
                return Page();
            }

            if (EmbedEnabled && origins.Count == 0)
            {
                EmbedError = "Add at least one website before turning the widget on.";
                ShowEmbedModal = true;
                return Page();
            }

            var embed = EmbedSettings ?? new KnowledgeBaseEmbed
            {
                KnowledgeBaseId = id,
                Token = EmbedChatController.CreateToken(),
                CreatedAt = DateTime.UtcNow
            };

            embed.AllowedOrigins = string.Join('\n', origins);
            embed.IsEnabled = EmbedEnabled;
            if (EmbedSettings == null)
            {
                _appDbContext.KnowledgeBaseEmbeds.Add(embed);
            }
            else if (regenerate)
            {
                embed.Token = EmbedChatController.CreateToken();
            }

            await _appDbContext.SaveChangesAsync();
            return RedirectToPage(new { id, embed = "open", saved = "1" });
        }

        private async Task<IActionResult?> LoadAsync(int id)
        {
            var userLogged = await GetCurrentUserAsync();
            if (userLogged == null)
            {
                return RedirectToPage("/login");
            }

            var knowledgeBase = await _appDbContext.KnowledgeBases.FirstOrDefaultAsync(kb =>
                (kb.UserId == userLogged.Id || kb.OrganizationId == userLogged.OrganizationId) && kb.Id == id);
            if (knowledgeBase == null)
            {
                return NotFound();
            }

            UserKnowledgeBase = knowledgeBase;
            KnowledgeBaseDocuments = await _appDbContext.Documents
                .Where(d => d.KnowledgeBaseId == knowledgeBase.Id && d.OrganizationId == userLogged.OrganizationId)
                .ToListAsync();
            EmbedSettings = await _appDbContext.KnowledgeBaseEmbeds
                .FirstOrDefaultAsync(e => e.KnowledgeBaseId == knowledgeBase.Id);
            return null;
        }

    }
}
