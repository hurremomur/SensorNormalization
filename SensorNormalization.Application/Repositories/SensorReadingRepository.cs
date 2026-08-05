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
        return await _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorType == sensorType)
            .OrderByDescending(r => r.Time)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestPerTypeAsync(
        CancellationToken cancellationToken)
    {
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
        IQueryable<SensorReading> query = _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorType == sensorType);

        if (fromUtc.HasValue)
            query = query.Where(r => r.Time >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(r => r.Time <= toUtc.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.Time)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int Count, double? Min, double? Max, double? Average)> GetSummaryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        // Tip + opsiyonel tarih araligi filtresi.
        IQueryable<SensorReading> query = _dbContext.SensorReadings
            .AsNoTracking()
            .Where(r => r.SensorType == sensorType);

        if (fromUtc.HasValue)
            query = query.Where(r => r.Time >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(r => r.Time <= toUtc.Value);

        int count = await query.CountAsync(cancellationToken);

        // Kayit yoksa min/max/avg hesaplanamaz -> null dondur.
        if (count == 0)
            return (0, null, null, null);

        // Toplama (aggregate) islemlerini veritabani yapar - hizli.
        double min = await query.MinAsync(r => r.Value, cancellationToken);
        double max = await query.MaxAsync(r => r.Value, cancellationToken);
        double avg = await query.AverageAsync(r => r.Value, cancellationToken);

        return (count, min, max, Math.Round(avg, 2));
    }
}
