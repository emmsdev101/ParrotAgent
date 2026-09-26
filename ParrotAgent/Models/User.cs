namespace ParrotAgent.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Role { get; set; } // e.g., "Admin", "User", etc
        public DateTime CreatedAt { get; set; }

    }
}
