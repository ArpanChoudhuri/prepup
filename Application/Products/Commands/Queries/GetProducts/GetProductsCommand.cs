using Infra.models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.Queries.GetProducts
{
    public sealed record GetProductsCommand(): IRequest<IReadOnlyCollection<ProductDto>>
    {
    }

    public sealed record GetProductByIdCommand(Guid Id):IRequest<ProductDto>
    { }
}
