using Application.Common;
using Application.Exceptions;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Net;
using System.Text.Json;
using WebApi.Contracts;
using WebApi.Resources;

namespace WebApi.Middleware
{
    public class GlobalExceptionHandlingMiddleware : IMiddleware
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandlingMiddleware(
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IStringLocalizer<SharedResource> localizer,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _localizer = localizer;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var id)
                ? id?.ToString() ?? context.TraceIdentifier
                : context.TraceIdentifier;

            HttpStatusCode statusCode;
            string errorCode;
            List<FieldError>? fieldErrors = null;

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorCode = ErrorCodes.ValidationError;
                    fieldErrors = validationException.Errors
                        .Select(e => new FieldError(e.PropertyName, ErrorCodes.ValidationError, e.ErrorMessage))
                        .ToList();
                    break;

                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorCode = notFoundException.ErrorCode;
                    break;

                case DomainException domainException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorCode = domainException.ErrorCode;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Forbidden;
                    errorCode = ErrorCodes.Forbidden;
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    errorCode = ErrorCodes.Unexpected;
                    break;
            }

            var response = new ErrorResponse(
                Success: false,
                ErrorCode: errorCode,
                Message: ResolveMessage(errorCode),
                TraceId: traceId,
                StatusCode: (int)statusCode,
                Errors: fieldErrors,
                // Only ever populated outside Production — never trust this being absent client-side
                // as a security boundary on its own; it exists for local/dev convenience only.
                DebugDetail: _environment.IsDevelopment() ? exception.ToString() : null);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }

        private string ResolveMessage(string errorCode)
        {
            var localized = _localizer[errorCode];
            // A project may introduce a new error code and forget to add its translation. Fail
            // safe to the generic "unexpected error" text instead of leaking the raw code to users.
            return localized.ResourceNotFound ? _localizer[ErrorCodes.Unexpected].Value : localized.Value;
        }
    }
}
