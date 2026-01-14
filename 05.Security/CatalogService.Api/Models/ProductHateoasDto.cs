namespace CatalogService.Api.Models;

/// <summary>
/// Represents a product resource with HATEOAS links for Level 3 RESTful compliance.
/// </summary>
public record ProductHateoasDto(
    int Id,
    string Name,
    string? Description = null,
    Uri? Image = null,
    decimal Price = 0m,
    int Amount = 0,
    int CategoryId = 0,
    IEnumerable<LinkDto>? Links = null)
{
    /// <summary>
    /// Gets the collection of HATEOAS links for related operations.
    /// </summary>
    public IEnumerable<LinkDto> Links { get; set; } = Links ?? [];
}