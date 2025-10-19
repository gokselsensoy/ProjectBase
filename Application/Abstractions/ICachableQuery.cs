using MediatR;

namespace Application.Abstractions
{
    public interface ICachableQuery<out TResponse> : IRequest<TResponse>
    {
        string CacheKey { get; }
        TimeSpan CacheDuration { get; }
    }
}
