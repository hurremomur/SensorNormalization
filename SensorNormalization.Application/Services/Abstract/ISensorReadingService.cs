using SensorNormalization.Application.Dto;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Services.Abstract;

public interface ISensorReadingService
{
    Task SaveAsync(SensorReading reading, CancellationToken cancellationToken);

    Task<IReadOnlyList<SensorReadingDto>> GetLatestPerTypeAsync(CancellationToken cancellationToken);
    Task<SensorReadingDto?> GetLatestByTypeAsync(SensorType sensorType, CancellationToken cancellationToken);
    Task<PagedResult<SensorReadingDto>> GetHistoryAsync(
        SensorType sensorType, DateTime? fromUtc, DateTime? toUtc,
        int pageIndex, int pageSize, CancellationToken cancellationToken);
    Task<SensorReadingSummaryDto> GetSummaryAsync(
        SensorType sensorType, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}
