using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data.Extensions;

namespace CatalogService.Infrastructure.Services;

public class CategoryService : ICatalogService<Category>
{
    private readonly IRepository<Category> _categoryRepository;

    public CategoryService(IRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public Task<Category?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        return _categoryRepository.GetItemAsync(id, cancellationToken);
    }

    public Task<IEnumerable<Category>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return _categoryRepository.GetItemsAsync(cancellationToken);
    }

    public async Task<int> AddItemAsync(Category category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        var existingCategory = await _categoryRepository.GetItemAsync(category.Id, cancellationToken);

        if (existingCategory != null)
        {
            throw new InvalidOperationException($"Category with ID {category.Id} already exists.");
        }

        return await _categoryRepository.AddItemAsync(category, cancellationToken);
    }

    public async Task<int> UpdateItemAsync(Category category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);

        var existingCategory = await _categoryRepository.GetItemAsync(category.Id, cancellationToken);

        if (existingCategory == null)
        {
            throw new InvalidOperationException($"Category with ID {category.Id} does not exist.");
        }

        return await _categoryRepository.UpdateItemAsync(category, cancellationToken);
    }

    public Task<int> DeleteItemAsync(int categoryId, CancellationToken cancellationToken)
    {
        return DeleteItemAsync(categoryId, cascadeDelete: false, cancellationToken);
    }

    public async Task<int> DeleteItemAsync(int categoryId, bool cascadeDelete, CancellationToken cancellationToken)
    {
        var existingCategory = await _categoryRepository.GetItemAsync(categoryId, cancellationToken);

        if (existingCategory == null)
        {
            throw new InvalidOperationException($"Category with ID {categoryId} does not exist.");
        }

        // Check if there are any products referencing this category
        var hasProducts = await _categoryRepository.HasProductsAsync(categoryId, cancellationToken);

        if (hasProducts && !cascadeDelete)
        {
            throw new InvalidOperationException($"Cannot delete category with ID {categoryId} because it has associated products. Please delete or reassign the products first, or use cascade delete.");
        }

        // If cascade delete is enabled, delete all associated products first
        if (cascadeDelete && hasProducts)
        {
            await _categoryRepository.DeleteProductsByCategoryIdAsync(categoryId, cancellationToken);
        }

        return await _categoryRepository.DeleteItemAsync(categoryId, cancellationToken);
    }
}