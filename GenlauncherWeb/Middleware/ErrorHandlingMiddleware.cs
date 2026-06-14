using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GenLauncherWeb.Middleware;

/// <summary>
/// Converts exceptions from the API into a JSON body of the shape { "error": "..." }
/// so the frontend always has something presentable to show.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException e)
        {
            await WriteError(context, e.StatusCode, e.Message);
        }
        catch (FileNotFoundException e)
        {
            await WriteError(context, 404, e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            await WriteError(context, 404, e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception for {Path}", context.Request.Path);
            await WriteError(context, 500, e.Message);
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(new { error = message }));
    }
}
