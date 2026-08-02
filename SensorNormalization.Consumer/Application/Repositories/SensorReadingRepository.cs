using SensorNormalization.Consumer.Infrastructure.Contexts;
using SensorNormalization.Domain.Entities;

namespace SensorNormalization.Consumer.Application.Repositories;

// ISensorReadingRepository''nin EF Core (TimescaleDB) uygulamasi.
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
}
