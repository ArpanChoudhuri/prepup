namespace Infra.models
{
    public sealed record ProductDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int UnitPrice { get; set; }
    }

    public sealed record OrderDto(
    Guid OrderId,
    DateTime OrderDate,
    decimal Total,
    string Status,
    IReadOnlyCollection<OrderItemDto> Items
);

    public sealed record OrderItemDto(
        ProductDto ProductDto,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );
}
