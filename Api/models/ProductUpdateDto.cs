namespace Api.models
{
    public class ProductUpdateDto
    {
        public record ProductUpdDto(string Name, decimal Price, byte[] RowVersion);
    }
}
