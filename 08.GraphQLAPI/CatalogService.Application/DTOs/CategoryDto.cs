namespace CatalogService.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Uri? Image { get; set; }
    public CategoryDto? Parent { get; set; }
    public IEnumerable<ProductDto>? Products { get; set; }
}
