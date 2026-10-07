namespace ParrotAgent.Models
{
    public class KnowledgeBaseEmbed
    {
        public int Id { get; set; }
        public int KnowledgeBaseId { get; set; }
        public KnowledgeBase KnowledgeBase { get; set; } = null!;
        public required string Token { get; set; }
        public string AllowedOrigins { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
