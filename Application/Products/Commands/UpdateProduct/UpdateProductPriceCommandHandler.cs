using Infra.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Products.Commands.UpdateProduct
{
    public class UpdateProductPriceHandler : IRequestHandler<UpdateProductPriceCommand, Unit>
    {
        private readonly AppDbContext _db;

        public UpdateProductPriceHandler(AppDbContext db) => _db = db;

        public async Task<Unit> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
        {
            // Load the target product
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
            if (product is null)
            {
                throw new KeyNotFoundException($"Product {request.ProductId} not found.");
            }

            // Convert rowVersion hex string to byte[] and attach as OriginalValue for concurrency check
            if (!string.IsNullOrWhiteSpace(request.RowVersion))
            {
                var original = ParseRowVersionHex(request.RowVersion);
                if (original != null)
                {
                    _db.Entry(product).Property(nameof(product.RowVersion)).OriginalValue = original;
                }
            }

            // Apply change and persist
            product.Price = request.NewPrice;
            await _db.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private static byte[]? ParseRowVersionHex(string hex)
        {
            // Accept "0x..." or plain hex. Return null for empty/whitespace.
            var s = hex.Trim();
            if (s.Length == 0) return null;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(2);
            }

            if (s.Length % 2 != 0)
            {
                // malformed
                throw new FormatException("RowVersion hex string has an odd length.");
            }

            var bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var pair = s.Substring(i * 2, 2);
                try
                {
                    bytes[i] = Convert.ToByte(pair, 16);
                }
                catch (FormatException)
                {
                    throw new FormatException($"RowVersion contains invalid hex characters: '{pair}'.");
                }
            }

            return bytes;
        }
    }
}
