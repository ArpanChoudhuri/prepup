using Infra.Data;
using Infra.models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.Queries.GetProducts
{
    public class GetProductsHandler : IRequestHandler<GetProductsCommand, IReadOnlyCollection<Infra.models.ProductDto>>
    {
        private readonly AppDbContext _db;
        public GetProductsHandler(AppDbContext db) {
            _db = db;
        }

        public async Task<IReadOnlyCollection<ProductDto>> Handle(GetProductsCommand request, CancellationToken cancellationToken)
        {
            var products = await _db.Products
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    IsActive = p.IsActive,
                    UnitPrice = p.UnitPrice
                }).ToListAsync(cancellationToken);

            return products;

        }
    }
}
