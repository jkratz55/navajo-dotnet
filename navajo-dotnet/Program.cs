using Microsoft.EntityFrameworkCore;
using navajo_dotnet.Data;
using navajo_dotnet.Service;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()) {
    builder.Configuration.AddUserSecrets<Program>();   
}

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

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

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();