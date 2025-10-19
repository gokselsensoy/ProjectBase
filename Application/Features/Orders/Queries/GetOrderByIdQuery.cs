using Application.Abstractions;
using MediatR;

namespace Application.Features.Orders.Queries
{
    public class GetOrderByIdQuery : ICachableQuery<OrderDto>
    {
        public Guid OrderId { get; set; }
        public string CacheKey => $"order:{OrderId}";
        public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
    }
}
