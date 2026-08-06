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

// --- CORS: frontend (Vue, localhost:8080) API''yi cagirabilsin ---
const string FrontendCors = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy =>
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// --- Web API + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS, MapControllers''dan once devreye girmeli.
app.UseCors(FrontendCors);

app.MapControllers();

app.Run();
