using System.Net;
using System.Text.Json;

namespace InstallmentBusiness.Api.Middleware;

// Maps the exception types thrown by the service layer to HTTP status codes,
// so individual controller actions don't need repetitive try/catch blocks.
//   KeyNotFoundException          -> 404
//   ArgumentException             -> 400
//   InvalidOperationException     -> 400  (business-rule violations, e.g.
//                                          "plan already Active", "amount
//                                          exceeds outstanding balance")
//   UnauthorizedAccessException   -> 401  (login/password failures --
//                                          NOT the same path as a missing/
//                                          invalid JWT on a protected route,
//                                          which the framework's own auth
//                                          middleware rejects before this
//                                          middleware ever sees a request)
//   anything else                 -> 500, with details logged but not
//                                          leaked to the client
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
