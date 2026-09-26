using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParrotAgent.Database;
using ParrotAgent.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace ParrotAgent.Services 
{

    public interface IDocumentProcessor
    {
        Task EmbedDocumentAsync(int documentId, string filePath);
    }
    public class DocumentBlock
    {
        public string Content { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 0;
        public string SourcePath { get; set; } = string.Empty;
    }
    public class DocumentProcessor : IDocumentProcessor
    {

        private readonly AppDbContext _appDbContext;
        private readonly ILogger<DocumentProcessor> _logger;
        
        public DocumentProcessor(AppDbContext appDbContext, ILogger<DocumentProcessor> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task EmbedDocumentAsync(int documentId, string filePath)
        {
            _logger.LogInformation("Starting to embed document Id: " + documentId);

            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found for documentId: {Id}", documentId);
                    return;
                }

                var document = await _appDbContext.Documents.FirstOrDefaultAsync(duc => duc.Id == documentId);

                string filename = document.Title;

                var documentBlocks = ExtractDocumentBlocks(filePath);

                List<string> documentChunks = new List<string>();

                foreach(var documentBlock in documentBlocks)
                {
                    var blockChunks = CreateSemanticChunks(documentBlock.Content);
                    string pageNumber = documentBlock.PageNumber.ToString();

                    foreach(var blockChunk in blockChunks)
                    {
                        string textToEmbed = $"[Source: {filename} | Page: {pageNumber}]\n{blockChunk}";
                        documentChunks.Add(textToEmbed);
                    }

                }

                //TODO:IMPLEMENT EMBEDDING AND SAVING CHUNKS TO DB

                // Write the documentChunks to json file for testing.

                string combinedText = string.Join("\n\n--- CHUNK SEPARATOR ---\n\n", documentChunks);

                await File.WriteAllTextAsync("output.text", combinedText);




            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error occured during embedding of documentId {}", documentId);
                throw;
            }
        }

        public List<DocumentBlock> ExtractDocumentBlocks(string filePath)
        {
            var documentBlocks = new List<DocumentBlock>();

            using var document = PdfDocument.Open(filePath);

            foreach(var page in document.GetPages())
            {
                var pageBlocks = RecursiveXYCut.Instance.GetBlocks(page.GetWords());

                string cleanedText = "";

                foreach (var block in pageBlocks)
                {
                    cleanedText += cleanText(block.Text);
                    cleanedText += "\n";

                }
                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    documentBlocks.Add(new DocumentBlock
                    {
                        Content = cleanedText,
                        PageNumber = page.Number,
                        SourcePath = filePath
                    });
                }
            }

            return documentBlocks;
        }

        public List<string> CreateSemanticChunks(string fullText, int maxChunkSize = 1000, int overlap = 200)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(fullText)) return chunks;

            int start = 0;
            while (start < fullText.Length)
            {
                int length = Math.Min(maxChunkSize, fullText.Length - start);
                string chunk = fullText.Substring(start, length);

                chunks.Add(chunk);

                // Move forward by chunk size minus overlap to retain context across boundaries
                start += (maxChunkSize - overlap);

                if (start >= fullText.Length) break;
            }

            return chunks;
        }

        public string cleanText(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text,@"\s+", " ");
            return text.Trim();
        }
    }
}

