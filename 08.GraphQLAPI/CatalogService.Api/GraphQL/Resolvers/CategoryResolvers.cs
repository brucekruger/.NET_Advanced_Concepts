using CatalogService.Api.GraphQL.DataLoaders;
using CatalogService.Application.DTOs;

namespace CatalogService.Api.GraphQL.Resolvers;

/// <summary>
/// Custom resolvers for the Category type.
/// Uses DataLoaders to efficiently resolve related products.
/// </summary>
public class CategoryResolvers
{
    /// <summary>
    /// Resolves the products for a category using DataLoader pattern.
    /// Prevents N+1 queries by batch-loading products.
    /// </summary>
    public Task<IEnumerable<ProductDto>?> GetProducts(
        [Parent] CategoryDto category,
        ProductBatchDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        return dataLoader.LoadAsync(category.Id, cancellationToken);
    }
}
