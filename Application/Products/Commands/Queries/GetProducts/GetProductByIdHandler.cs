using Infra.Data;
using Infra.models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.Queries.GetProducts
{
    public class GetProductByIdHandler : IRequestHandler<GetProductByIdCommand, ProductDto>
    {
        private readonly AppDbContext _db;
        public GetProductByIdHandler(AppDbContext db) 
        {
            _db = db;
        }
        public Task<ProductDto> Handle(GetProductByIdCommand request, CancellationToken cancellationToken)
        {
            var product = _db.Products
                .Where(p => p.Id == request.Id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    IsActive = p.IsActive,
                    UnitPrice = p.UnitPrice
                })
                .FirstOrDefault();
            if (product != null)
            {
                return Task.FromResult(product);
            }
            return Task.FromResult<ProductDto>(null);
        }
    }
}
