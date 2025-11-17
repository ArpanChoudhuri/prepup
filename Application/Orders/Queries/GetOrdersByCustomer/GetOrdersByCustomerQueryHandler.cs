using Application.Orders.Queries.GetOrdersByCustomer;
using Application.Orders.Queries.GetOrdersQuery;
using Infra.Data;
using Infra.models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class GetOrdersByCustomerQueryHandler
    : IRequestHandler<GetOrdersByCustomerQuery, IReadOnlyCollection<OrderDto>>
{
    private readonly AppDbContext _db;

    public GetOrdersByCustomerQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<OrderDto>> Handle(
        GetOrdersByCustomerQuery request,
        CancellationToken ct)
    {
        // AsNoTracking to improve performance for read-only queries
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == request.CustomerId)
            .OrderByDescending(o => o.OrderDate)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync(ct);

        return orders
            .Select(o => new OrderDto(
                o.Id,
                o.OrderDate,
                o.Total,
                o.Status,
                o.Items.Select(i => new OrderItemDto(
                    new ProductDto 
                    { 
                        Id=i.Product.Id, 
                        IsActive=i.Product.IsActive, 
                        Name=i.Product.Name, 
                        Price=i.Product.Price,
                        UnitPrice=i.Product.UnitPrice
                    },
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                )).ToList()
            ))
            .ToList();
    }
}
