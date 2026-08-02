using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SensorNormalization.Consumer.Infrastructure.Contexts;

// Sadece tasarim zamani (dotnet ef migrations/database) icin kullanilir.
// Uygulama calisirken devrede degildir; o zaman DI ayarlari saglar.
public class SensorDbContextFactory : IDesignTimeDbContextFactory<SensorDbContext>
{
    public SensorDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SensorDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=sensordb;Username=postgres;Password=postgres");

        return new SensorDbContext(optionsBuilder.Options);
    }
}
