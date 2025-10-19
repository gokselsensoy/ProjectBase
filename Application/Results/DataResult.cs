namespace Application.Results
{
    public class DataResult<T> : Result, IDataResult<T>
    {
        public T Data { get; }

        // Protected constructor: Sadece miras alan sınıflar kullanabilir.
        protected DataResult(T data, bool success, string message) : base(success, message)
        {
            Data = data;
        }

        protected DataResult(T data, bool success) : base(success)
        {
            Data = data;
        }
    }
}
