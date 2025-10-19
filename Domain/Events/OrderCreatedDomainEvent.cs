﻿using MediatR;

namespace Domain.Events
{
    public class OrderCreatedDomainEvent : INotification
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
