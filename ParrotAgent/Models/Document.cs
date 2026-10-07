namespace ParrotAgent.Models
{
    public class Document
    {

        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Metadata { get; set; }
        public required string ContentType { get; set; }
        public required string FilePath { get; set; }
        public required int OrganizationId { get; set; }
        public required int KnowledgeBaseId { get; set; } // Foreign key to KnowledgeBase
        public required int UserId { get; set; } // Foreign key to User
        public required string Status { get; set; } // Status of the document (e.g., "active", "archived")
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
