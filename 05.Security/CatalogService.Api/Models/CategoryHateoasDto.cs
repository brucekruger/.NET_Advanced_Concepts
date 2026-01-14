namespace CatalogService.Api.Models;

/// <summary>
/// Represents a category resource with HATEOAS links for Level 3 RESTful compliance.
/// </summary>
public record CategoryHateoasDto(
    int Id,
    string Name,
    Uri? Image = null,
    int? ParentId = null,
    IEnumerable<LinkDto>? Links = null)
{
    /// <summary>
    /// Gets the collection of HATEOAS links for related operations.
    /// </summary>
    public IEnumerable<LinkDto> Links { get; set; } = Links ?? [];
}