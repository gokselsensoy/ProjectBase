using Application.Abstractions;

namespace Application.Features.Orders.Commands.UpdateAddress
{
    public class UpdateOrderAddressCommand : ICommand
    {
        public Guid OrderId { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }

        // TODO: Burası için de bir Validator sınıfı oluşturulmalı
        // (UpdateOrderAddressCommandValidator.cs)
    }
}
