namespace CatalogService.Api.GraphQL.InputTypes;

/// <summary>
/// Input type for creating a new category.
/// </summary>
/// <param name="Name">The name of the category.</param>
/// <param name="Image">Optional image URL for the category.</param>
/// <param name="ParentId">Optional parent category identifier.</param>
public record CreateCategoryInput(
    string Name,
    Uri? Image = null,
    int? ParentId = null);
