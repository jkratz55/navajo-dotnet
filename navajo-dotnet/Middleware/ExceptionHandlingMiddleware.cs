using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace navajo_dotnet.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            ProblemDetails resp = new  ProblemDetails   
            {
                Title = "An error occurred",
                Detail = _environment.IsDevelopment() ? ex.Message : "An unexpected error occurred. Please try again later.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            };
            
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            resp.Extensions["traceId"] = traceId;

            if (_environment.IsDevelopment())
            {
                resp.Extensions["exceptionType"] = ex.GetType().FullName;
                resp.Extensions["stackTrace"] = ex.StackTrace;
                resp.Extensions["source"] = ex.Source;
            }
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(resp);
        }
    }
}