using Microsoft.EntityFrameworkCore;
using AppFramework.Data.Sqlite.Entities;

namespace AppFramework.Data.Sqlite;

/// <summary>
/// DbContext المحلي (SQLite).
/// </summary>
public sealed class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options)
        : base(options)
    {
    }

    /// <summary>سجلات الكيانات.</summary>
    public DbSet<LocalRecord> Records => Set<LocalRecord>();

    /// <summary>مدخلات Outbox.</summary>
    public DbSet<OutboxEntry> Outbox => Set<OutboxEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ============ LocalRecord ============
        mb.Entity<LocalRecord>(e =>
        {
            e.ToTable("LocalRecords");
            e.HasKey(x => x.Id);

            e.Property(x => x.SourceKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.ItemKey).HasMaxLength(256).IsRequired();
            e.Property(x => x.ClrType).HasMaxLength(512).IsRequired();
            e.Property(x => x.Payload).IsRequired();

            e.HasIndex(x => new { x.SourceKey, x.ItemKey }).IsUnique();
            e.HasIndex(x => x.UpdatedAt);
        });

        // ============ OutboxEntry ============
        mb.Entity<OutboxEntry>(e =>
        {
            e.ToTable("Outbox");
            e.HasKey(x => x.Id);

            e.Property(x => x.SourceKey).HasMaxLength(128).IsRequired();
            e.Property(x => x.ItemKey).HasMaxLength(256);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.SourceKey);
        });
    }
}