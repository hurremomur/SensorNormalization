using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Repositories;

public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);

    Task<SensorReading?> GetLatestByTypeAsync(SensorType sensorType, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReading>> GetLatestPerTypeAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<SensorReading> Items, int TotalCount)> GetHistoryAsync(
        SensorType sensorType, DateTime? fromUtc, DateTime? toUtc,
        int pageIndex, int pageSize, CancellationToken cancellationToken);

    Task<(int Count, double? Min, double? Max, double? Average)> GetSummaryAsync(
        SensorType sensorType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);

    // Istatistiksel anomali icin: bir tipin son N degerini (en yeni once) getirir.
    Task<IReadOnlyList<double>> GetRecentValuesAsync(
        SensorType sensorType, int count, CancellationToken cancellationToken);
}
