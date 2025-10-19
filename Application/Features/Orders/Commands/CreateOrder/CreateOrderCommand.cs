using Application.Abstractions;

namespace Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommand : ICommand<Guid>
    {
        public Guid CustomerId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
