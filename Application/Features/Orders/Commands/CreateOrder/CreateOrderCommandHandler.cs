using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using Domain.SeedWork;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper; // (Opsiyonel, DTO -> Entity için)

        public CreateOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Domain validasyonu için Value Object oluştur
            var shippingAddress = new Address(request.Street, request.City, request.ZipCode);

            // 2. Aggregate Root'u (Order) factory metoduyla oluştur
            var newOrder = Order.Create(request.CustomerId, shippingAddress);

            // 3. Domain kurallarını (iş mantığını) çalıştır
            foreach (var item in request.OrderItems)
            {
                newOrder.AddOrderItem(item.ProductId, item.Quantity, item.Price);
            }

            // 4. Repository ile veritabanına (in-memory) ekle
            _orderRepository.Add(newOrder);

            // 5. UnitOfWork ile tüm değişiklikleri tek bir transaction'da kaydet
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. Sonucu dön
            return newOrder.Id;
        }
    }
}
