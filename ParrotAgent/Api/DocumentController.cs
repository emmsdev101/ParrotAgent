using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotAgent.Database;
using ParrotAgent.Models;
using ParrotAgent.Services;
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

        public DocumentController(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
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


    }
}
