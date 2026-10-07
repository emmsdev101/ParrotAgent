namespace ParrotAgent.Models
{
    public class Role
    {
        public int Id { get; set; }
        public required string Name { get; set; } // e.g., "Admin", "User", etc
    }
}