using Microsoft.EntityFrameworkCore;
using SensorNormalization.Application.Infrastructure.Contexts;
using SensorNormalization.Application.Repositories;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;
using Xunit;

namespace SensorNormalization.Tests;

// Gercek TimescaleDB''ye yazip okuyan entegrasyon testleri.
// NOT: Bu testlerin calismasi icin Docker/TimescaleDB ayakta olmalidir
// (docker compose up -d). Test verileri "TEST-" onekiyle isaretlenir ve
// her testin sonunda temizlenir.
public class SensorReadingRepositoryIntegrationTests : IDisposable
{
    private readonly SensorDbContext _dbContext;
    private readonly SensorReadingRepository _repository;

    public SensorReadingRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SensorDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sensordb;Username=postgres;Password=postgres")
            .Options;

        _dbContext = new SensorDbContext(options);
        _repository = new SensorReadingRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_kaydi_veritabanina_yazar()
    {
        // Arrange - benzersiz test kaydi
        var reading = NewTestReading(SensorType.Temperature, 25.5, "C");

        // Act
        await _repository.AddAsync(reading, CancellationToken.None);

        // Assert - geri oku, gercekten yazilmis mi
        var saved = await _dbContext.SensorReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reading.Id);

        Assert.NotNull(saved);
        Assert.Equal(25.5, saved!.Value);
        Assert.Equal("C", saved.Unit);
        Assert.Equal(SensorType.Temperature, saved.SensorType);
    }

    [Fact]
    public async Task GetLatestByTypeAsync_en_son_kaydi_getirir()
    {
        // Arrange - ayni tipten iki kayit, farkli zamanlarda
        var older = NewTestReading(SensorType.Pressure, 1000.0, "hPa");
        older.Time = DateTime.UtcNow.AddMinutes(-10);
        var newer = NewTestReading(SensorType.Pressure, 1013.0, "hPa");
        newer.Time = DateTime.UtcNow;

        await _repository.AddAsync(older, CancellationToken.None);
        await _repository.AddAsync(newer, CancellationToken.None);

        // Act
        var latest = await _repository.GetLatestByTypeAsync(SensorType.Pressure, CancellationToken.None);

        // Assert - en son (newer) gelmeli; ama baska testlerden de Pressure olabilir,
        // o yuzden en azindan newer''in zamanindan eski olmadigini dogrula
        Assert.NotNull(latest);
        Assert.True(latest!.Time >= newer.Time.AddSeconds(-1));
    }

    [Fact]
    public async Task GetSummaryAsync_dogru_istatistik_hesaplar()
    {
        // Arrange - benzersiz bir test tipi yerine, yazdigimiz kayitlarin
        // araligini kontrol edelim. Iki humidity kaydi yaziyoruz.
        var from = DateTime.UtcNow;
        var r1 = NewTestReading(SensorType.Humidity, 40.0, "%");
        var r2 = NewTestReading(SensorType.Humidity, 60.0, "%");
        await _repository.AddAsync(r1, CancellationToken.None);
        await _repository.AddAsync(r2, CancellationToken.None);
        var to = DateTime.UtcNow;

        // Act - sadece bu testin araligindaki humidity ozeti
        var (count, min, max, avg) = await _repository.GetSummaryAsync(
            SensorType.Humidity, from, to, CancellationToken.None);

        // Assert - en az bizim 2 kaydimiz var, min<=40, max>=60
        Assert.True(count >= 2);
        Assert.True(min <= 40.0);
        Assert.True(max >= 60.0);
    }

    // Benzersiz, isaretli test kaydi olusturur.
    private static SensorReading NewTestReading(SensorType type, double value, string unit) => new()
    {
        Id = Guid.NewGuid(),
        Time = DateTime.UtcNow,
        SensorId = "TEST-" + Guid.NewGuid().ToString("N")[..8],
        SensorType = type,
        Value = value,
        Unit = unit,
        SourceFormat = PayloadFormat.Json,
        RawPayload = "integration-test",
        ReceivedAtUtc = DateTime.UtcNow
    };

    // Test sonunda: bu testin yazdigi TEST- kayitlarini temizle.
    public void Dispose()
    {
        var testRows = _dbContext.SensorReadings
            .Where(r => r.SensorId.StartsWith("TEST-"));
        _dbContext.SensorReadings.RemoveRange(testRows);
        _dbContext.SaveChanges();
        _dbContext.Dispose();
    }
}
