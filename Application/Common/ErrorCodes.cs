namespace Application.Common
{
    /// <summary>
    /// Generic, language-agnostic error codes the client can pattern-match on.
    /// Handlers/domain code should only ever produce a code from here (or a project-specific
    /// extension of it) — never a hardcoded, language-specific message. The actual user-facing
    /// text is resolved once, centrally, in GlobalExceptionHandlingMiddleware via IStringLocalizer.
    /// </summary>
    public static class ErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFound = "NOT_FOUND";
        public const string DomainRuleViolation = "DOMAIN_RULE_VIOLATION";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string Unexpected = "UNEXPECTED_ERROR";
    }
}
