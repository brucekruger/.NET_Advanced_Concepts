namespace CatalogService.Api.GraphQL.InputTypes;

/// <summary>
/// Input type for updating an existing category.
/// </summary>
/// <param name="Id">The identifier of the category to update.</param>
/// <param name="Name">The new name for the category.</param>
/// <param name="Image">Optional new image URL for the category.</param>
/// <param name="ParentId">Optional new parent category identifier.</param>
public record UpdateCategoryInput(
    int Id,
    string Name,
    Uri? Image = null,
    int? ParentId = null);
