using MediatR;

namespace Application.Products.Commands.UpdateProduct
{
    public sealed record UpdateProductPriceCommand : IRequest<Unit>
    {
        public Guid ProductId { get; set; }
        public int NewPrice { get; set; }

        // Accept row version as a string (hex like "0x000000000000A416" or "000000000000A416")
        public string? RowVersion { get; set; }
    }
}
