using Microsoft.EntityFrameworkCore;
using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Application.Infrastructure.Contexts;

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
        entity.HasKey(e => new { e.Id, e.Time });

        entity.Property(e => e.SensorType).HasConversion<string>().HasMaxLength(32);
        entity.Property(e => e.SourceFormat).HasConversion<string>().HasMaxLength(16);
        entity.Property(e => e.SensorId).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Unit).HasMaxLength(16).IsRequired();

        entity.Property(e => e.Time);
        entity.Property(e => e.ReceivedAtUtc);
        entity.Property(e => e.IsAnomaly);

        entity.HasIndex(e => e.Time);
    }
}
