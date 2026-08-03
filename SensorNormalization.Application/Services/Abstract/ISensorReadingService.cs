using SensorNormalization.Application.Dto;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Services.Abstract;

// Normalize okumalarin is katmani: yazma + okuma.
// Consumer yazma tarafini, Api (Reporting) okuma tarafini kullanir.
public interface ISensorReadingService
{
    // Yazma (Consumer kullanir)
    Task SaveAsync(SensorReading reading, CancellationToken cancellationToken);

    // Okuma (Api kullanir)
    Task<IReadOnlyList<SensorReadingDto>> GetLatestPerTypeAsync(CancellationToken cancellationToken);
    Task<SensorReadingDto?> GetLatestByTypeAsync(SensorType sensorType, CancellationToken cancellationToken);
    Task<PagedResult<SensorReadingDto>> GetHistoryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
