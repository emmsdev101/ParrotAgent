using Microsoft.EntityFrameworkCore;
using ParrotAgent.Models;

namespace ParrotAgent.Database
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
        {
            _configuration = configuration;
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
                string connectionString = _configuration["ExternalDbConnection"]??"";
                optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());  
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<DocumentVector>(entity =>
            {
                entity.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            });

            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                // Set up the relationship (One Document/DocumentVector to Many Chunks)
                entity.HasOne(c => c.Document)
                      .WithMany(d => d.Chunks)
                      .HasForeignKey(c => c.DocumentId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Double check that the data type is assigned accurately
                entity.Property(c => c.Embedding)
                      .HasColumnType("vector(1536)");

            });

        
        }
    }
}
