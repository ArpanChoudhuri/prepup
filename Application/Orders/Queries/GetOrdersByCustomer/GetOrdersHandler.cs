using Application.Orders.Queries.GetOrdersQuery;
using Infra.Data;
using Infra.models;
using Infra.Persistence.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Orders.Queries.GetOrdersByCustomer
{
    public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrderQuery, IReadOnlyCollection<OrderDto>>
    {
        private readonly AppDbContext _db;

        public GetOrdersQueryHandler(AppDbContext db)
        {
            _db = db;
        }
        public async Task<IReadOnlyCollection<OrderDto>> Handle(GetOrdersByCustomerQuery request, CancellationToken cancellationToken)
        {
            var orders =  OrderQueries.GetLatestOrdersByCustomer(_db, request.CustomerId).ToBlockingEnumerable();

            // map to DTO
            return orders.Select(o => new OrderDto(
                o.Id,
                o.OrderDate,
                o.Total,
                o.Status,
                o.Items.Select(i => new OrderItemDto(
                    new ProductDto
                    {
                        Id = i.Product.Id,
                        IsActive = i.Product.IsActive,
                        Name = i.Product.Name,
                        Price = i.Product.Price,
                        UnitPrice = i.Product.UnitPrice
                    },
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                )).ToList())).ToList();
        }

        public async Task<IReadOnlyCollection<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            var orders = await _db.Orders
                        .AsNoTracking()
                        .Where(o => o.Id == request.id)
                        .OrderByDescending(o => o.OrderDate)
                        .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                        .ToListAsync(cancellationToken);

            // map to DTO
            return orders.Select(o => new OrderDto(
                o.Id,
                o.OrderDate,
                o.Total,
                o.Status,
                o.Items.Select(i => new OrderItemDto(
                    new ProductDto
                    {
                        Id = i.Product.Id,
                        IsActive = i.Product.IsActive,
                        Name = i.Product.Name,
                        Price = i.Product.Price,
                        UnitPrice = i.Product.UnitPrice
                    },
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                )).ToList())).ToList();
        }
    }
}

