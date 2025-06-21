using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace navajo_dotnet.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport result)
    {
        context.Response.ContentType = "application/json";
        
        var response = new HealthResponse
        {
            Status = result.Status.ToString(),
            Checks = result.Entries.Select(entry => new HealthCheckItem
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                Duration = FormatDuration(entry.Value.Duration),
                Data = entry.Value.Data
            }),
            Duration = FormatDuration(result.TotalDuration),
            Timestamp = DateTimeOffset.UtcNow
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
    
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F3}s";
        return $"{duration.TotalMinutes:F1}m";
    }

}