using Domain.SeedWork;

namespace Domain.Events
{
    public class OrderUpdatedDomainEvent : IDomainEvent
    {
        public Guid OrderId { get; }
        public Guid CustomerId { get; }

        public OrderUpdatedDomainEvent(Guid orderId, Guid customerId)
        {
            OrderId = orderId;
            CustomerId = customerId;
        }
    }
}
