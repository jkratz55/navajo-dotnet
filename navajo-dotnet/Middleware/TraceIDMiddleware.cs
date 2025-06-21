using System.Diagnostics;

namespace navajo_dotnet.Middleware;

public class TraceIDMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIDMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            context.Response.Headers["X-Trace-ID"] = activity.TraceId.ToString();
        }
        
        await _next(context);
    }
}