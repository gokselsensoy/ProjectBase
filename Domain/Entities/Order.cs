using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.SeedWork;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public class Order : Entity, IAggregateRoot
    {
        public Guid CustomerId { get; private set; }
        public Address ShippingAddress { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedDate { get; private set; }

        private readonly List<OrderItem> _orderItems = new();
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

        // Dışarıdan 'new Order()' yapılmasını engelliyoruz.
        private Order() { }

        // Factory Metodu: Bir Order yaratmanın tek yolu budur.
        // Bu metot, bir Order'ın yaratılması için GEREKLİ olan tüm
        // domain kurallarını uygular.
        public static Order Create(Guid customerId, Address shippingAddress)
        {
            if (customerId == Guid.Empty)
                throw new OrderDomainException("Customer ID boş olamaz.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Sipariş yaratıldığında bir event fırlat (belki email atılır?)
            order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, order.CustomerId));

            return order;
        }

        // Bu, Aggregate'in içindeki bir başka domain kuralı
        public void AddOrderItem(Guid productId, int quantity, decimal price)
        {
            if (quantity <= 0)
                throw new OrderDomainException("Miktar 0'dan büyük olmalıdır.");

            var existingItem = _orderItems.FirstOrDefault(o => o.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.AddQuantity(quantity);
            }
            else
            {
                _orderItems.Add(new OrderItem(Id, productId, quantity, price));
            }
        }

        public void SetAsShipped()
        {
            if (Status != OrderStatus.Pending)
                throw new OrderDomainException("Sadece bekleyen siparişler kargolanabilir.");

            Status = OrderStatus.Shipped;
        }
    }
}
