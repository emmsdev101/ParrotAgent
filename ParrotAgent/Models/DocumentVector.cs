using Microsoft.Data.SqlTypes; // Contains SqlVector
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParrotAgent.Models
{
    public class DocumentVector
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public List<DocumentChunk> Chunks { get; set; } = new();
    }

    public class DocumentChunk
    {
        public int Id { get; set; }

        // Foreign key back to the main file
        public int DocumentId { get; set; }
        public DocumentVector Document { get; set; } = null!;

        // The raw text content of this chunk
        public string TextContent { get; set; } = string.Empty;

        // Optional structure tracking (e.g., page 3, section 2)
        public int ChunkIndex { get; set; }

        public SqlVector<float> Embedding { get; set; }
    }

}