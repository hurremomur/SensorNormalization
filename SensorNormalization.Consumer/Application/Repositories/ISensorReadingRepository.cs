using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Consumer.Application.Repositories;

// Normalize edilmis okumalarin kalici olarak saklanmasindan sorumlu katman.
// Consumer dogrudan veritabanini degil, bu soyutlamayi kullanir.
public interface ISensorReadingRepository
{
    Task AddAsync(SensorReading reading, CancellationToken cancellationToken);
}
