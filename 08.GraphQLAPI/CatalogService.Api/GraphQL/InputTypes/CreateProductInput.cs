namespace CatalogService.Api.GraphQL.InputTypes;

/// <summary>
/// Input type for creating a new product.
/// </summary>
/// <param name="Name">The product name.</param>
/// <param name="Description">Optional description of the product.</param>
/// <param name="Image">Optional image URL for the product.</param>
/// <param name="Price">The product price.</param>
/// <param name="Amount">The available amount in stock.</param>
/// <param name="CategoryId">The category identifier this product belongs to.</param>
public record CreateProductInput(
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId);
