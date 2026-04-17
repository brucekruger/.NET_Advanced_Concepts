namespace CatalogService.Api.Filters;

public class ProductFilter
{
    public int? CategoryId { get; set; }
    public int? PageSize { get; set; }
    public int? PageNum { get; set; }
}
