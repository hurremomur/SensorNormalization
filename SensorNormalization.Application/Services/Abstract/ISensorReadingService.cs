using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Application.Services.Abstract;

// Normalize edilmis okumanin is katmani sozlesmesi.
// Consumer dogrudan repository''e degil, bu servise konusur.
public interface ISensorReadingService
{
    Task SaveAsync(SensorReading reading, CancellationToken cancellationToken);
}
