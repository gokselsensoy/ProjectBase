namespace Domain.Exceptions
{
    public class DomainException : Exception
    {
        /// <summary>
        /// Language-agnostic code the API layer uses to resolve a localized message.
        /// Defaults to the generic domain-rule-violation code; subclasses can pass a more
        /// specific one (e.g. "ORDER_ALREADY_SHIPPED") that a project-specific resource file resolves.
        /// </summary>
        public string ErrorCode { get; }

        public DomainException() : this("Domain rule violation.", "DOMAIN_RULE_VIOLATION") { }

        public DomainException(string message) : this(message, "DOMAIN_RULE_VIOLATION") { }

        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = "DOMAIN_RULE_VIOLATION";
        }

        public DomainException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
