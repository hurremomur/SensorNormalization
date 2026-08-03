using Microsoft.EntityFrameworkCore;
using SensorNormalization.Application.Infrastructure.Contexts;
using SensorNormalization.Application.Repositories;
using SensorNormalization.Application.Services.Abstract;
using SensorNormalization.Application.Services.Concrete;

var builder = WebApplication.CreateBuilder(args);

// --- Veritabani (ortak Application katmanindaki context) ---
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SensorDb")));

// --- Repository + Service (ortak katman) ---
builder.Services.AddScoped<ISensorReadingRepository, SensorReadingRepository>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();

// --- Web API + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Gelistirme ortaminda Swagger arayuzu.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
