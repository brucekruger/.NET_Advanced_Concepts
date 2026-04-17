using CatalogService.Api.Filters;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;

namespace CatalogService.Api.Extensions;

public static class CatalogServiceExtensions
{
    public static async Task<IEnumerable<Product>> GetProductsByCategoryPagedAsync(this ICatalogService<Product> productService,
        ProductFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(productService);

        var allProducts = (await productService.GetItemsAsync(cancellationToken)).AsQueryable();

        if (filter.CategoryId.HasValue)
        {
            var parentCategory = await productService.GetItemAsync(filter.CategoryId.Value, cancellationToken);

            if (parentCategory == null)
            {
                throw new BadHttpRequestException($"Parent category with ID {filter.CategoryId.Value} does not exist.");
            }

            allProducts = allProducts.Where(p => p.CategoryId == parentCategory.Id);
        }

        if (filter is { PageSize: not null, PageNum: not null })
        {
            allProducts = allProducts.Skip(filter.PageSize.Value * (filter.PageNum.Value - 1))
                .Take(filter.PageSize.Value);
        }

        return allProducts.ToArray();
    }
}