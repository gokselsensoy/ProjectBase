using Application.Features.Orders.Queries.Shared.DTOs;
using Application.Features.Orders.Queries.Shared.Pagination;
using AutoMapper;
using MediatR;

namespace Application.Features.Orders.Queries.GetOrdersByCustomer
{
    public class GetOrdersByCustomerQueryHandler : IRequestHandler<GetOrdersByCustomerQuery, PaginatedResponse<OrderDto>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetOrdersByCustomerQueryHandler(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedResponse<OrderDto>> Handle(GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Orders
                .Where(o => o.CustomerId == request.CustomerId);

            if (request.StartDate.HasValue)
            {
                query = query.Where(o => o.CreatedDate >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(o => o.CreatedDate <= request.EndDate.Value);
            }

            // IQueryable'ı DTO'ya yansıt
            var dtoQuery = query
                .OrderByDescending(o => o.CreatedDate)
                .ProjectTo<OrderDto>(_mapper.ConfigurationProvider);

            // Sorguyu veritabanına göndermeden önce pagination'ı uygula
            // PaginatedResponse.CreateAsync metodu bizim için Count() ve Skip().Take() yapacak
            return await PaginatedResponse<OrderDto>.CreateAsync(
                dtoQuery,
                request.PageNumber,
                request.PageSize,
                cancellationToken
            );
        }
    }
}
