namespace WebApi.Contracts
{
    /// <summary>
    /// The single error shape every failure path in this API returns — thrown exceptions
    /// (via GlobalExceptionHandlingMiddleware) and auth challenges/forbids (via JwtBearerEvents
    /// in Program.cs) alike. Mobile/frontend clients should only ever need to branch on
    /// ErrorCode, never parse Message (which is localized and can change wording).
    /// </summary>
    public record ErrorResponse(
        bool Success,
        string ErrorCode,
        string Message,
        string TraceId,
        int StatusCode,
        List<FieldError>? Errors = null,
        string? DebugDetail = null);

    public record FieldError(string Field, string ErrorCode, string Message);
}
