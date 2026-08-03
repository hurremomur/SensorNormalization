using Microsoft.EntityFrameworkCore;
using SensorNormalization.Application.Infrastructure.Contexts;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Repositories;

public class SensorReadingRepository : ISensorReadingRepository
{
    private readonly SensorDbContext _dbContext;

    public SensorReadingRepository(SensorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(SensorReading reading, CancellationToken cancellationToken)
    {
        _dbContext.SensorReadings.Add(reading);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SensorReading?> GetLatestByTypeAsync(
        SensorType sensorType, CancellationToken cancellationToken)
    {
        // O tipe ait en son (Time''a gore) kayit.
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorType == sensorType)
            .OrderByDescending(r => r.Time)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestPerTypeAsync(
        CancellationToken cancellationToken)
    {
        // Her sensor tipi icin en son kayit. Tip sayisi az (3) oldugundan
        // tip basina ayri "en son" sorgusu nettir ve hypertable index''ini kullanir.
        var result = new List<SensorReading>();
        foreach (SensorType type in Enum.GetValues<SensorType>())
        {
            var latest = await GetLatestByTypeAsync(type, cancellationToken);
            if (latest is not null)
                result.Add(latest);
        }
        return result;
    }

    public async Task<(IReadOnlyList<SensorReading> Items, int TotalCount)> GetHistoryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Temel sorgu: tip filtresi + opsiyonel tarih araligi.
        IQueryable<SensorReading> query = _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorType == sensorType);

        if (fromUtc.HasValue)
            query = query.Where(r => r.Time >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(r => r.Time <= toUtc.Value);

        // Once toplam sayi (sayfalama meta verisi icin).
        int totalCount = await query.CountAsync(cancellationToken);

        // Sonra sayfa: en yeni once, pageIndex/pageSize ile.
        var items = await query
            .OrderByDescending(r => r.Time)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
