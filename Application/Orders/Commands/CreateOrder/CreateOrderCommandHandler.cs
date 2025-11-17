using Infra.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Orders.Commands.CreateOrder
{
    public sealed class CreateOrderCommandHandler
        : IRequestHandler<CreateOrderCommand, CreateOrderResult>
    {
        private readonly AppDbContext _db;

        public CreateOrderCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken ct)
        {
            // 1. load all needed products in one query
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id) && p.IsActive)
                .ToListAsync(ct);

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("One or more products are invalid or inactive.");

            // 2. create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = "Created",
                Tenant = "TEN1ANT1"
            };

            decimal total = 0m;

            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                var lineTotal = product.UnitPrice * item.Quantity;

                order.Items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    Product = product,
                    Quantity = item.Quantity,
                    UnitPrice = product.UnitPrice,
                    LineTotal = lineTotal
                });

                total += lineTotal;
            }

            order.Total = total;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);

            return new CreateOrderResult(order.Id);
        }
    }
}
