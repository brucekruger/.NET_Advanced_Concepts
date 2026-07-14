using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for efficiently batch-loading categories to prevent N+1 queries.
/// Implements the DataLoader pattern using GreenDonut.
/// </summary>
public class CategoryBatchDataLoader : BatchDataLoader<int, CategoryDto>
{
    private readonly IApplicationDbContext _dbContext;
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryBatchDataLoader"/> class.
    /// </summary>
    /// <param name="batchScheduler">The batch scheduler instance provided by the GraphQL runtime.</param>
    /// <param name="dbContext">The application database context used to load categories.</param>
    /// <param name="options">Optional data loader options.</param>
    public CategoryBatchDataLoader(
        IBatchScheduler batchScheduler,
        IApplicationDbContext dbContext,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads multiple categories in a single database query.
    /// </summary>
    protected override async Task<IReadOnlyDictionary<int, CategoryDto>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .Include(c => c.Parent)
            .Where(c => keys.Contains(c.Id))
            .ToArrayAsync(cancellationToken);

        // Map domain entities to DTOs
        var dtoDictionary = categories.ToDictionary(
            c => c.Id,
            c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Image = c.Image,
                Parent = c.Parent == null ? null : new CategoryDto
                {
                    Id = c.Parent.Id,
                    Name = c.Parent.Name,
                    Image = c.Parent.Image
                },
                // Products are intentionally not loaded here to avoid deep recursion
                Products = null
            });

        return dtoDictionary;
    }
}
