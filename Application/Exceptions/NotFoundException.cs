using Application.Common;

namespace Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public string ErrorCode { get; }

        public NotFoundException(string message) : base(message)
        {
            ErrorCode = ErrorCodes.NotFound;
        }

        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.")
        {
            ErrorCode = ErrorCodes.NotFound;
        }

        public NotFoundException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
