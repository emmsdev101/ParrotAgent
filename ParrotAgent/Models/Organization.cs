namespace ParrotAgent.Models
{
    public class Organization
    {
        public int Id { get; set; }
        public string Domain { get; set; } = string.Empty;
        public required string Name { get; set; } // e.g., "Admin", "User", etc
        public ICollection<User> Users { get; set; } = new List<User>();
        public required string Status { get; set; } = "active";
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}