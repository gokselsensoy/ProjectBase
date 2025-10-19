namespace Application.Results
{
    public class SuccessDataResult<T> : DataResult<T>
    {
        public SuccessDataResult(T data, string message) : base(data, true, message) { }
        public SuccessDataResult(T data) : base(data, true) { } // Mesajsız
        public SuccessDataResult(string message) : base(default!, true, message) { } // Sadece mesaj
        public SuccessDataResult() : base(default!, true) { } // Sadece başarı durumu
    }
}
