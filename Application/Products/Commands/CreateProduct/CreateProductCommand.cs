using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.CreateProduct
{
    public sealed record CreateProductCommand(
        string Name,
        decimal Price,
        bool IsActive
    ) : IRequest<CreateProductResult>;

    public sealed record CreateProductResult(Guid ProductId);

}
