using Infra.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.CreateProduct
{
    public class CreateProductsHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
    {
        private readonly AppDbContext _db;
        public CreateProductsHandler(AppDbContext db)
        {
            _db = db;
        }   
 
        public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            Guid newProductId = Guid.NewGuid();
            Product newProduct = new Product
            {
                Id = newProductId,
                Name = request.Name,
                Price = request.Price,
                IsActive = request.IsActive
            };
            _db.Products.Add(newProduct);
            await _db.SaveChangesAsync(cancellationToken);
            return await Task.FromResult(new CreateProductResult(newProductId));

        }
    }
}
