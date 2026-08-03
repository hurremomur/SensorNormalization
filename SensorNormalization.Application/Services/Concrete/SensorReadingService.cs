using SensorNormalization.Application.Repositories;
using SensorNormalization.Application.Services.Abstract;
using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Application.Services.Concrete;

// ISensorReadingService uygulamasi.
// Kalici yazma oncesi denetim alanini (ReceivedAtUtc) doldurur, sonra repository''e delege eder.
public class SensorReadingService : ISensorReadingService
{
    private readonly ISensorReadingRepository _repository;

    public SensorReadingService(ISensorReadingRepository repository)
    {
        _repository = repository;
    }

    public async Task SaveAsync(SensorReading reading, CancellationToken cancellationToken)
    {
        // Kaydin sisteme islendigi an (UTC).
        reading.ReceivedAtUtc = DateTime.UtcNow;

        await _repository.AddAsync(reading, cancellationToken);
    }
}
