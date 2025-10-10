using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Services;

public class CategoryService : ICatalogService<Category>
{
    private readonly IRepository<Category> _categoryRepository;

    public CategoryService(IRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
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

    public async Task<int> DeleteItemAsync(int categoryId, CancellationToken cancellationToken)
    {
        var existingCategory = await _categoryRepository.GetItemAsync(categoryId, cancellationToken);

        if (existingCategory == null)
        {
            throw new InvalidOperationException($"Category with ID {categoryId} does not exist.");
        }

        return await _categoryRepository.DeleteItemAsync(categoryId, cancellationToken);
    }
}