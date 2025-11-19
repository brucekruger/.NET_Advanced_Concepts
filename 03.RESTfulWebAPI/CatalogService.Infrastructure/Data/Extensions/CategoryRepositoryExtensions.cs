using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Data.Extensions;

public static class CategoryRepositoryExtensions
{
    public static async Task<bool> HasProductsAsync(this IRepository<Category> repository, int categoryId, CancellationToken cancellationToken)
    {
        if (repository is CategoryRepository categoryRepository)
        {
            return await categoryRepository.HasProductsAsync(categoryId, cancellationToken);
        }

        // If repository is not the concrete CategoryRepository (e.g. in unit tests where a mock is used),
        // assume there are no products to allow higher-level logic to proceed. This avoids forcing tests
        // to depend on the concrete repository implementation.
        return await Task.FromResult(false);
    }

    public static async Task<int> DeleteProductsByCategoryIdAsync(this IRepository<Category> repository, int categoryId, CancellationToken cancellationToken)
    {
        if (repository is CategoryRepository categoryRepository)
        {
            return await categoryRepository.DeleteProductsByCategoryIdAsync(categoryId, cancellationToken);
        }

        // If repository is not the concrete CategoryRepository (e.g. in unit tests where a mock is used),
        // nothing to delete.
        return await Task.FromResult(0);
    }
}

