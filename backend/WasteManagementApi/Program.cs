using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using WasteManagementApi.Data;
using WasteManagementApi.Middleware;
using WasteManagementApi.Services;
using WasteManagementApi.Telemetry;

// --- Serilog early bootstrap logger ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- Controllers & API ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- Database ---
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";
if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<WasteManagementDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")
            ?? "Host=localhost;Port=5432;Database=waste-management;Username=postgres;Password=postgres"));
}
else
{
    builder.Services.AddDbContext<WasteManagementDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=waste-management.db"));
}

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5231")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// --- Custom Metrics (singleton) ---
builder.Services.AddSingleton<WasteManagementMetrics>();

// --- Simulation state (singleton) ---
builder.Services.AddSingleton<SimulationStateService>();

// --- Background workers ---
builder.Services.AddHostedService<SensorSimulatorService>();
builder.Services.AddHostedService<AlertMonitorService>();

// --- OpenTelemetry ---
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("waste-management-api", serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddSource(WasteManagementActivitySource.SourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddMeter(WasteManagementMetrics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

// --- Kestrel port ---
builder.WebHost.UseUrls("http://0.0.0.0:5230");

var app = builder.Build();

// --- Seed database ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WasteManagementDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(db, logger);
}

// --- Middleware pipeline ---
app.MapOpenApi();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
