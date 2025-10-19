namespace Application.Results
{
    public class ErrorDataResult<T> : DataResult<T>
    {
        public ErrorDataResult(T data, string message) : base(data, false, message) { }
        public ErrorDataResult(T data) : base(data, false) { } // Mesajsız
        public ErrorDataResult(string message) : base(default!, false, message) { } // Sadece mesaj
        public ErrorDataResult() : base(default!, false) { } // Sadece hata durumu
    }
}
