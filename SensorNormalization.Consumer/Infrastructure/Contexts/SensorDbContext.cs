using Microsoft.EntityFrameworkCore;
using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Consumer.Infrastructure.Contexts;

// Uygulamanin veritabani ile konustugu koprü (EF Core DbContext).
public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SensorReading>();

        entity.ToTable("sensor_readings");

        // Composite PK: TimescaleDB hypertable partition kolonu (Time) PK''nin parcasi olmali.
        entity.HasKey(e => new { e.Id, e.Time });

        entity.Property(e => e.SensorType).HasConversion<string>().HasMaxLength(32);
        entity.Property(e => e.SourceFormat).HasConversion<string>().HasMaxLength(16);
        entity.Property(e => e.SensorId).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Unit).HasMaxLength(16).IsRequired();

        entity.Property(e => e.Time);
        entity.Property(e => e.ReceivedAtUtc);

        entity.HasIndex(e => e.Time);
    }
}
