using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Repositories;

// Normalize okumalarin kalici katmani. Yazma + okuma sozlesmeleri.
public interface ISensorReadingRepository
{
    // Yazma
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);

    // Okuma - belirli tipin en son kaydi (yoksa null)
    Task<SensorReading?> GetLatestByTypeAsync(SensorType sensorType, CancellationToken cancellationToken);

    // Okuma - her tip icin en son kayit (dashboard ozeti)
    Task<IReadOnlyList<SensorReading>> GetLatestPerTypeAsync(CancellationToken cancellationToken);

    // Okuma - sayfali gecmis (tarih araligi opsiyonel). toplam sayi + sayfa ogeleri.
    Task<(IReadOnlyList<SensorReading> Items, int TotalCount)> GetHistoryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);
}
