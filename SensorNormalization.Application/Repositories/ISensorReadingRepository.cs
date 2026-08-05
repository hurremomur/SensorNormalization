using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Repositories;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);

    Task<SensorReading?> GetLatestByTypeAsync(SensorType sensorType, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetLatestPerTypeAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<SensorReading> Items, int TotalCount)> GetHistoryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);

    // Belirli tipin araliktaki istatistigi: adet, min, max, ortalama.
    Task<(int Count, double? Min, double? Max, double? Average)> GetSummaryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
