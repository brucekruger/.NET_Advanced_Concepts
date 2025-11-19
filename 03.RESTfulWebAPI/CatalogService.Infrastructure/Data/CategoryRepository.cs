using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Data;

public class CategoryRepository : IRepository<Category>
{
    private readonly IApplicationDbContext _applicationDbContext;

    public CategoryRepository()
    {
    }

    public CategoryRepository(IApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
    }

    public async Task<Category?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var category = await _applicationDbContext.Categories.FindAsync([id], cancellationToken);
        return category;
    }

    public async Task<IEnumerable<Category>> GetItemsAsync(CancellationToken cancellationToken)
    {
        var categories = await _applicationDbContext.Categories
            .AsNoTracking()
            .Include(c => c.Parent)
            .ToArrayAsync(cancellationToken);

        return categories;
    }

    public async Task<int> AddItemAsync(Category item, CancellationToken cancellationToken)
    {
        await _applicationDbContext.Categories.AddAsync(item, cancellationToken);
        var added = await _applicationDbContext.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<int> UpdateItemAsync(Category item, CancellationToken cancellationToken)
    {
        _applicationDbContext.Categories.Update(item);
        var updated = await _applicationDbContext.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<int> DeleteItemAsync(int itemId, CancellationToken cancellationToken)
    {
        var category = await _applicationDbContext.Categories.FindAsync([itemId], cancellationToken);
        if (category != null)
        {
            _applicationDbContext.Categories.Remove(category);
            return await _applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        return 0;
    }

    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.Products
            .AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
    }

    public async Task<int> DeleteProductsByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
    {
        var products = await _applicationDbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .ToArrayAsync(cancellationToken);

        if (products.Any())
        {
            _applicationDbContext.Products.RemoveRange(products);
            return await _applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        return 0;
    }
}