namespace Api.models
{
    public class ProductDtos
    {
        public record ProductListItem(int Id, string Name, decimal Price, Byte[] RowVersion);
    }
}
