namespace ParrotAgent.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required int RoleId { get; set; }
        public required Role Role { get; set; }
        public required string Status { get; set; } = "active";
        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
