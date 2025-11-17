using Infra.models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Orders.Queries.GetOrdersQuery
{
    public sealed record GetOrdersByCustomerQuery(string CustomerId)
        : IRequest<IReadOnlyCollection<OrderDto>>;

    public sealed record GetOrderQuery(Guid id)
    : IRequest<IReadOnlyCollection<OrderDto>>;


}
