using CatalogService.Api.Models;

namespace CatalogService.Api.Interfaces;

/// <summary>
/// Service for generating HATEOAS links for API resources.
/// Supports Level 3 of the Richardson Maturity Model.
/// </summary>
public interface IHateoasLinkBuilder
{
    /// <summary>
    /// Builds HATEOAS links for a category resource.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>An enumerable of <see cref="LinkDto"/> for category operations.</returns>
    IEnumerable<LinkDto> BuildCategoryLinks(int categoryId);

    /// <summary>
    /// Builds HATEOAS links for a product resource.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="categoryId">The category ID the product belongs to.</param>
    /// <returns>An enumerable of <see cref="LinkDto"/> for product operations.</returns>
    IEnumerable<LinkDto> BuildProductLinks(int productId, int categoryId);

    /// <summary>
    /// Builds HATEOAS links for a collection of categories.
    /// </summary>
    /// <returns>An enumerable of <see cref="LinkDto"/> for category collection operations.</returns>
    IEnumerable<LinkDto> BuildCategoryCollectionLinks();

    /// <summary>
    /// Builds HATEOAS links for a collection of products.
    /// </summary>
    /// <returns>An enumerable of <see cref="LinkDto"/> for product collection operations.</returns>
    IEnumerable<LinkDto> BuildProductCollectionLinks();
}