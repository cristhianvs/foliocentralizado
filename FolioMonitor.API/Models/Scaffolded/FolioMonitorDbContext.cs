using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace FolioMonitor.API.Models.Scaffolded;

public partial class FolioMonitorDbContext : DbContext
{
    public FolioMonitorDbContext()
    {
    }

    public FolioMonitorDbContext(DbContextOptions<FolioMonitorDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Configuration> Configurations { get; set; }

    public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; }

    public virtual DbSet<Foliohistory> Foliohistories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=FolioMonitorDb;uid=folio_api_user;pwd=developer;allowpublickeyretrieval=True", Microsoft.EntityFrameworkCore.ServerVersion.Parse("5.6.40-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("PRIMARY");

            entity.ToTable("configurations");

            entity.Property(e => e.Key).HasMaxLength(100);
        });

        modelBuilder.Entity<Efmigrationshistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__efmigrationshistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Foliohistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("foliohistories");

            entity.HasIndex(e => new { e.Modulo, e.Timestamp }, "IX_FolioHistories_Modulo_Timestamp");

            entity.HasIndex(e => e.Timestamp, "IX_FolioHistories_Timestamp");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.FechaActualizacion).HasMaxLength(6);
            entity.Property(e => e.FechaRegistro).HasMaxLength(6);
            entity.Property(e => e.FolioActual).HasColumnType("int(11)");
            entity.Property(e => e.FolioFin).HasColumnType("int(11)");
            entity.Property(e => e.FolioInicio).HasColumnType("int(11)");
            entity.Property(e => e.FoliosDisponibles).HasColumnType("int(11)");
            entity.Property(e => e.Timestamp).HasMaxLength(6);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
