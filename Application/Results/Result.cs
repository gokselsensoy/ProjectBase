namespace Application.Results
{
    public class Result : IResult
    {
        public bool Success { get; }
        public string Message { get; }

        // Protected constructor: Sadece miras alan sınıflar kullanabilir.
        protected Result(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        protected Result(bool success) // Mesajsız constructor
        {
            Success = success;
            Message = string.Empty; // Default mesaj
        }
    }
}
