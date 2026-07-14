namespace CatalogService.Api.GraphQL.InputTypes;

/// <summary>
/// Input type for updating an existing product.
/// </summary>
/// <param name="Id">The product identifier.</param>
/// <param name="Name">The product name.</param>
/// <param name="Description">Optional product description.</param>
/// <param name="Image">Optional image URL for the product.</param>
/// <param name="Price">The product price.</param>
/// <param name="Amount">The available stock amount.</param>
/// <param name="CategoryId">The category identifier the product belongs to.</param>
public record UpdateProductInput(
    int Id,
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId);
