using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Utilities;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ParrotAgent.Api
{
    [ApiController]
    [Route("api/embed")]
    public class EmbedChatController : Controller
    {
        public const int MaxMessageLength = 1000;
        private readonly AppDbContext _context;
        private readonly IEmbedder _embedder;
        private readonly ILLM _llm;

        public record EmbedAskRequest(string? Message);

        public EmbedChatController(AppDbContext context, IEmbedder embedder, ILLM llm)
        {
            _context = context;
            _embedder = embedder;
            _llm = llm;
        }

        [HttpOptions("{token}/ask")]
        public Task<IActionResult> Preflight(string token) => HandleAsync(token, null, preflight: true);

        [HttpPost("{token}/ask")]
        public Task<IActionResult> Ask(string token, [FromBody] EmbedAskRequest? request) => HandleAsync(token, request, preflight: false);

        private async Task<IActionResult> HandleAsync(string token, EmbedAskRequest? request, bool preflight)
        {
            Response.Headers.CacheControl = "no-store";

            if (string.IsNullOrWhiteSpace(token) || token.Length > 80)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var originHeader = Request.Headers.Origin.ToString();
            var embed = await _context.KnowledgeBaseEmbeds
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Token == token);

            if (embed == null || !embed.IsEnabled || !EmbedOriginRules.IsAllowed(originHeader, embed.AllowedOrigins))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!EmbedOriginRules.TryNormalize(originHeader, out var origin, out _))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            Response.Headers.Append("Access-Control-Allow-Origin", origin);
            Response.Headers.Append("Vary", "Origin");
            Response.Headers.Append("Access-Control-Allow-Methods", "POST, OPTIONS");
            Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");
            Response.Headers.Append("Access-Control-Max-Age", "600");

            if (preflight || HttpMethods.IsOptions(Request.Method))
            {
                return NoContent();
            }

            var message = request?.Message?.Trim() ?? string.Empty;
            if (message.Length == 0 || message.Length > MaxMessageLength)
            {
                return BadRequest(new { error = $"Ask a question of 1–{MaxMessageLength} characters." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!EmbedRateLimiter.IsAllowed($"embed:{token}:{ip}", 20, TimeSpan.FromMinutes(1))
                || !EmbedRateLimiter.IsAllowed($"embed:{token}", 60, TimeSpan.FromMinutes(1)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many messages. Try again in a minute." });
            }

            var safeQuestion = message.Replace("<", string.Empty, StringComparison.Ordinal)
                .Replace(">", string.Empty, StringComparison.Ordinal);

            try
            {
                var embeddedQueryData = await _embedder.GetEmbeddingsAsync(new List<string> { safeQuestion });
                if (embeddedQueryData == null || embeddedQueryData.Length == 0)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The assistant is unavailable right now." });
                }

                float[] queryVectors = embeddedQueryData[0].Embedding;
                var documentChunks = await _context.DocumentChunks
                    .AsNoTracking()
                    .Where(c => c.Document.KnowledgeBaseId == embed.KnowledgeBaseId)
                    .Select(c => new
                    {
                        c.TextContent,
                        Distance = c.Embedding.CosineDistance(new Vector(queryVectors))
                    })
                    .Where(x => x.Distance <= 0.5)
                    .OrderBy(x => x.Distance)
                    .Take(8)
                    .ToListAsync();

                var contextChunks = documentChunks.Select(d => d.TextContent).ToList();
                var answer = await _llm.RagChat(contextChunks, safeQuestion);
                if (string.IsNullOrWhiteSpace(answer))
                {
                    answer = "I couldn't find an answer in this knowledge base.";
                }

                if (answer.Length > 4000)
                {
                    answer = answer[..4000];
                }

                return Json(new { answer });
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The assistant is unavailable right now." });
            }
        }

        public static string CreateToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
