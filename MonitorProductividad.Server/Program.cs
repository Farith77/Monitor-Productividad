using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MonitorProductividad.Server.Data;
using MonitorProductividad.Server.Endpoints;
using MonitorProductividad.Server.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la fuente de datos PostgreSQL habilitando JSON Dinámico
var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson(); // <-- ¡La línea mágica que soluciona el crasheo!
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseNpgsql(dataSource));

// 2. Inyección de dependencias de nuestros Servicios
builder.Services.AddScoped<ITelemetryIngestionService, TelemetryIngestionService>();
builder.Services.AddScoped<IMetricsAnalyticsService, MetricsAnalyticsService>();
builder.Services.AddHostedService<DataRetentionBackgroundService>();

// 3. Habilitar CORS (Permite localhost en desarrollo y el dominio de Vercel en producción)
var allowedOrigin = builder.Configuration["AllowedFrontendOrigin"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5173", allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// 4. Aplicar los middlewares
app.UseCors("FrontendPolicy");

// 5. Mapeo modular de Endpoints
app.MapGroup("/api/telemetria").MapTelemetriaEndpoints();
app.MapGroup("/api/dashboard").MapDashboardEndpoints();
app.MapGroup("/api/dispositivos").MapDispositivosEndpoints();

app.Run();