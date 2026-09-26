using Microsoft.EntityFrameworkCore;
using ParrotAgent.Models;

namespace ParrotAgent.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Pattern 2: Explicitly required by "dotnet ef migrations" at design-time 
        // to bypass DI initialization failures
        public AppDbContext()
        {
        }

        // Define your DbSets (tables) here
        // Example:
        // public DbSet<User> Users { get; set; })
        public DbSet<User> Users { get; set; }
        public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
        public DbSet<Document> Documents { get; set; }

        public DbSet<DocumentVector> DocumentVectors { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=aspnet-53bc9b9d-9d6a-45d4-8429-2a2761773502;Trusted_Connection=True;MultipleActiveResultSets=true");

            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                // Set up the relationship (One Document/DocumentVector to Many Chunks)
                entity.HasOne(c => c.Document)
                      .WithMany(d => d.Chunks)
                      .HasForeignKey(c => c.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Double check that the data type is assigned accurately
                entity.Property(c => c.Embedding)
                      .HasColumnType("vector(1536)");

            });
        }
    }
}
