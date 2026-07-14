using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for efficiently batch-loading products by category to prevent N+1 queries.
/// Implements the DataLoader pattern using GreenDonut.
/// </summary>
public class ProductBatchDataLoader : BatchDataLoader<int, IEnumerable<ProductDto>>
{
    private readonly IApplicationDbContext _dbContext;
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductBatchDataLoader"/> class.
    /// </summary>
    /// <param name="batchScheduler">The batch scheduler used by the DataLoader.</param>
    /// <param name="dbContext">The application database context used to load products.</param>
    /// <param name="options">Optional DataLoader options.</param>
    public ProductBatchDataLoader(
        IBatchScheduler batchScheduler,
        IApplicationDbContext dbContext,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads all products grouped by category in a single database query.
    /// </summary>
    protected override async Task<IReadOnlyDictionary<int, IEnumerable<ProductDto>>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        var products = await this._dbContext.Products
            .Where(p => keys.Contains(p.CategoryId))
            .ToArrayAsync(cancellationToken);

        // Map domain products to DTOs and group by CategoryId
        var productDtos = products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Image = p.Image,
                Price = p.Price,
                Amount = p.Amount,
                CategoryId = p.CategoryId
            })
            .ToList();

        return keys.ToDictionary(
            k => k,
            k => productDtos.Where(p => p.CategoryId == k).AsEnumerable());
    }
}
