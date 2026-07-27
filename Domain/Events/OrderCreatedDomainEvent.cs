using Domain.SeedWork;

namespace Domain.Events
{
    public class OrderCreatedDomainEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public Guid CustomerId { get; }

        public OrderCreatedDomainEvent(Guid orderId, Guid customerId)
        {
            OrderId = orderId;
            CustomerId = customerId;
        }
    }
}
