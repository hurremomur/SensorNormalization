using SensorNormalization.Application.Dto;
using SensorNormalization.Application.Repositories;
using SensorNormalization.Domain.Entities;
using SensorNormalization.Domain.Messages;

namespace SensorNormalization.Application.Services.Concrete;

public class SensorReadingService : Abstract.ISensorReadingService
{
    private readonly ISensorReadingRepository _repository;

    public SensorReadingService(ISensorReadingRepository repository)
    {
        _repository = repository;
    }

    // --- YAZMA ---
    public async Task SaveAsync(SensorReading reading, CancellationToken cancellationToken)
    {
        // Kaydin sisteme islendigi an (UTC).
        reading.ReceivedAtUtc = DateTime.UtcNow;
        await _repository.AddAsync(reading, cancellationToken);
    }

    // --- OKUMA ---
    public async Task<IReadOnlyList<SensorReadingDto>> GetLatestPerTypeAsync(
        CancellationToken cancellationToken)
    {
        var readings = await _repository.GetLatestPerTypeAsync(cancellationToken);
        return readings.Select(MapToDto).ToList();
    }

    public async Task<SensorReadingDto?> GetLatestByTypeAsync(
        SensorType sensorType, CancellationToken cancellationToken)
    {
        var reading = await _repository.GetLatestByTypeAsync(sensorType, cancellationToken);
        return reading is null ? null : MapToDto(reading);
    }

    public async Task<PagedResult<SensorReadingDto>> GetHistoryAsync(
        SensorType sensorType,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetHistoryAsync(
            sensorType, fromUtc, toUtc, pageIndex, pageSize, cancellationToken);

        return new PagedResult<SensorReadingDto>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.Select(MapToDto).ToList()
        };
    }

    // Entity -> DTO donusumu (ic modeli dis dunyadan ayirir).
    private static SensorReadingDto MapToDto(SensorReading r) => new()
    {
        SensorId = r.SensorId,
        SensorType = r.SensorType.ToString(),
        Value = r.Value,
        Unit = r.Unit,
        Time = r.Time,
        SourceFormat = r.SourceFormat.ToString()
    };
}
