using FolioMonitor.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FolioMonitor.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FolioHistory> FolioHistories { get; set; }
    public DbSet<Configuration> Configurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Configuration entity to use Key as the primary key
        modelBuilder.Entity<Configuration>().HasKey(c => c.Key);

        // Add any other specific configurations here if needed
        // e.g., indexing for faster queries on FolioHistory
        modelBuilder.Entity<FolioHistory>()
            .HasIndex(fh => fh.Timestamp);
        modelBuilder.Entity<FolioHistory>()
            .HasIndex(fh => new { fh.Modulo, fh.Timestamp });
    }
} 