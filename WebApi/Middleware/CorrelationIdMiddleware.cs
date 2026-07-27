using Serilog.Context;

namespace WebApi.Middleware
{
    /// <summary>
    /// Resolves a per-request correlation id (from an incoming X-Correlation-Id header, or the
    /// framework-generated TraceIdentifier otherwise), pushes it into every Serilog log line for
    /// the request, and echoes it back on the response so a client can hand it to support.
    /// GlobalExceptionHandlingMiddleware reads the same value for the "traceId" field on error responses.
    /// </summary>
    public class CorrelationIdMiddleware : IMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string HttpContextItemKey = "CorrelationId";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
                && !string.IsNullOrWhiteSpace(headerValue)
                    ? headerValue.ToString()
                    : context.TraceIdentifier;

            context.Items[HttpContextItemKey] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next(context);
            }
        }
    }
}
