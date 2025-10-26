﻿using Application.Features.Orders.DTOs;
using Application.Features.Orders.Queries.GetOrdersByCustomer;
using Application.Shared.Pagination;

namespace Application.Abstractions.EntityRepositories
{
    public interface IOrderQueryRepository
    {
        /// <summary>
        /// Bir siparişi ID'sine göre DTO olarak getirir.
        /// </summary>
        Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Müşteri ve tarih filtresine göre siparişleri sayfalı DTO olarak getirir.
        /// </summary>
        Task<PaginatedResponse<OrderDto>> GetOrdersByCustomerAsync(
            GetOrdersByCustomerQuery query,
            CancellationToken cancellationToken = default);
    }
}
