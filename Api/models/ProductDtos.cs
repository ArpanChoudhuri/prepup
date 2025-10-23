namespace Api.models
{
    public class ProductDtos
    {
        public record ProductListItem(int Id, string Name, decimal Price, Byte[]? RowVersion)
        {
            public ProductListItem(int id, string name, decimal price)
            {
                Id = id;
                Name = name;
                Price = price;
            }
        }
    }
}
