using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Orders.Commands.CreateOrder
{
    public sealed record CreateOrderCommand(
        string CustomerId,
        IReadOnlyCollection<CreateOrderItemDto> Items
    ) : IRequest<CreateOrderResult>;

    public sealed record CreateOrderItemDto(
        Guid ProductId,
        int Quantity
    );

    public sealed record CreateOrderResult(Guid OrderId);
}
