namespace DocumentService.Data;

using Microsoft.EntityFrameworkCore;
using DocumentService.Models;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<ProcessedDocument> Documents => Set<ProcessedDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedDocument>()
            .Property(b => b.Metadata)
            .HasColumnType("jsonb");
    }
}