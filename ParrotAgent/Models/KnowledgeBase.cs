namespace ParrotAgent.Models
{
    public class KnowledgeBase
    {

        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required int UserId { get; set; } // Foreign key to User
        public required string Color { get; set; } // Color for the knowledge base
        public required string Icon { get; set; } // Icon for the knowledge base
        public required string Visibility { get; set; } // Visibility of the knowledge base (e.g., "public", "private")
        public bool AllowMemberUploads { get; set; } = false; // Whether members can upload to the knowledge base
        public bool AllowMemberEditing { get; set; } = false; // Whether members can edit the knowledge base
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
