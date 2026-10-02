using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Models;
using ParrotAgent.Services;
using ParrotAgent.Utilities;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace ParrotAgent.Api
{
    [ApiController]
    [Route("api/knowledge-base")]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmbedder _embedder;
        private readonly ILLM _llm;

        public record AskRequest(string Message);
        public record Citation(string type, string name);

        public DocumentController(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, IEmbedder embedder, ILLM llm)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _embedder = embedder;
            _llm = llm;
        }

        public async void EmbedDocument(int id, string filePath)
        {
            

            if (!System.IO.File.Exists(filePath)) Console.WriteLine("File does not exists");

            string contents = "";
            await foreach (string line in System.IO.File.ReadLinesAsync(filePath))
            {
                contents += line;
            }

            
        }


        //api/knowledge-base/3/upload-document
        [HttpPost("{id}/upload-document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(int id, [FromForm] IFormFile file, IJobQueue queue)
        {
            int knowledgeBaseId = int.Parse((string)RouteData.Values["id"]);
            string userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded or the file is empty.");
            }

            var rawFileName = Path.GetFileName(file.FileName);
            var safeFileName = $"{Guid.NewGuid()}_{rawFileName}";

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "UploadedDocuments");

            try
            {
                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Id = 0, // Assuming the database will auto-generate this
                    Title = rawFileName,
                    Metadata = $"{{\"size\": {file.Length}, \"uploadedBy\": \"{userId}\"}}",
                    FilePath = filePath,
                    ContentType = file.ContentType,
                    Status = "Uploaded",
                    KnowledgeBaseId = knowledgeBaseId,
                    UserId = int.Parse(userId),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                queue.Enqueue<IDocumentProcessor>(processor => processor.EmbedDocumentAsync(document.Id, filePath));
                

                // 6. Return response with Document ID so the frontend can track background work
                return Ok(new
                {
                    documentId = document.Id,
                    fileName = document.Title,
                    message = "File uploaded successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error saving file: {ex.Message}");
            }
        }
        //[Authorize]
        [HttpPost("{id}/ask")]
        public async Task<IActionResult> Ask(int id, [FromBody] AskRequest askRequest)
        {
            string query = askRequest.Message;

            string userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            User user = await _context.Users?.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            var embeddedQueryData = await _embedder.GetEmbeddingsAsync(new List<string>{
                query
            });

            float[] queryVectors = embeddedQueryData[0].Embedding;

            var documentChunks = await _context.DocumentChunks
                .Where(c => c.Document.UserId == int.Parse(userId) && c.Document.KnowledgeBaseId == id)
                .Select(c => new
                    {
                        Chunk = c,

                        Distance = EF.Functions.VectorDistance(
                            "cosine",
                            c.Embedding,
                            new Microsoft.Data.SqlTypes.SqlVector<float>(queryVectors)
                            )

                    })
                .Where(x => x.Distance <= 0.5)
                .OrderBy(x => x.Distance)
                .Take(30)
                .ToListAsync();

            List<string> contextChunks = documentChunks.Select(d => d.Chunk.TextContent).ToList();
       

            string answer = await _llm.RagChat(contextChunks, query);

            var rs = new
            {
                answer = answer
            };


            return Json(rs);
        }
    
    }
}
