using Application.Features.Orders.Queries.Shared.DTOs;
using AutoMapper;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;

        public GetOrderByIdQueryHandler(IOrderRepository orderRepository, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);

            if (order == null)
            {
                // Not: Bu null durumu, global exception handler'da veya
                // controller'da "NotFound" olarak ele alınmalıdır.
                // Şimdilik null dönebiliriz veya bir "NotFoundException" fırlatabiliriz.
                // En iyisi özel bir exception fırlatmaktır, middleware'imiz bunu yakalar.
                throw new Exception($"Order with id {request.OrderId} not found."); // Basitlik için
            }

            // AutoMapper ile Order -> OrderDto dönüşümü yap
            var orderDto = _mapper.Map<OrderDto>(order);

            return orderDto;
        }
    }
}
