using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Data;

public class ProductRepository : IRepository<Product>
{
    private readonly IApplicationDbContext _applicationDbContext;

    public ProductRepository(IApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
    }

    public async Task<Product?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _applicationDbContext.Products.FindAsync([id], cancellationToken);
        return product;
    }

    public async Task<IEnumerable<Product>> GetItemsAsync(CancellationToken cancellationToken)
    {
        var products = await _applicationDbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToArrayAsync(cancellationToken);

        return products;
    }

    public async Task<int> AddItemAsync(Product item, CancellationToken cancellationToken)
    {
        await _applicationDbContext.Products.AddAsync(item, cancellationToken);
        var added = await _applicationDbContext.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<int> UpdateItemAsync(Product item, CancellationToken cancellationToken)
    {
        _applicationDbContext.Products.Update(item);
        var updated = await _applicationDbContext.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<int> DeleteItemAsync(int itemId, CancellationToken cancellationToken)
    {
        var product = await _applicationDbContext.Products.FindAsync([itemId], cancellationToken);
        if (product != null)
        {
            _applicationDbContext.Products.Remove(product);
            return await _applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        return 0;
    }
}