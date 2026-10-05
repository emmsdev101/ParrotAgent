using Pgvector;

namespace ParrotAgent.Models
{
    public class DocumentVector
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public List<DocumentChunk> Chunks { get; set; } = new();
        public User User { get; set; } = null!;
        public int UserId { get; set; }
        public int KnowledgeBaseId { get; set; }
    }

    public class DocumentChunk
    {
        public int Id { get; set; }

        // Foreign key back to the main file
        public int DocumentId { get; set; }
        public DocumentVector Document { get; set; } = null!;
       
        public string TextContent { get; set; } = string.Empty;

        // Optional structure tracking (e.g., page 3, section 2)
        public int ChunkIndex { get; set; }

        public Vector Embedding { get; set; }
    }

}