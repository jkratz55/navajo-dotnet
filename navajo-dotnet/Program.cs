using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using navajo_dotnet.Data;
using navajo_dotnet.Health;
using navajo_dotnet.Middleware;
using navajo_dotnet.Service;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

// If running in development mode, load user secrets
if (builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>();   
}

// Configure core .NET WebAPI components
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure entity framework and DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure dependency injection
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// Configure health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(
        builder.Configuration["ConnectionStrings:DefaultConnection"]!,
        name: "database",
        tags: new[] { "db", "sql", "postgresql" });

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(serviceName: "navajo", serviceVersion: "0.1.0"))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("NavajoMeter")
            .AddPrometheusExporter())
    .WithTracing(tracing =>
    {
        tracing
            .SetSampler(new AlwaysOnSampler())
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.EnrichWithException = (activity, exception) =>
                {
                    activity.SetTag("exception.message", exception.Message);
                    activity.SetTag("exception.stacktrace", exception.StackTrace);
                };
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(opts =>
            {
                opts.SetDbStatementForText = true;
                opts.SetDbStatementForStoredProcedure = true;
            })
            .AddOtlpExporter(opts =>
            {
                opts.Protocol = OtlpExportProtocol.HttpProtobuf;
                opts.Endpoint = new Uri("http://localhost:4318/v1/traces"); // Default OTLP endpoint for Jaeger
            });
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Setup WebAPI configuration and components including Prometheus scraping endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseMiddleware<TraceIDMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Register health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.Run();