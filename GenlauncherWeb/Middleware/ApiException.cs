using System;

namespace GenLauncherWeb.Middleware;

/// <summary>
/// Exception carrying an HTTP status code, turned into a JSON error response
/// by <see cref="ErrorHandlingMiddleware"/>.
/// </summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
