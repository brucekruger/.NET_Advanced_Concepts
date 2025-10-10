using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;

namespace CatalogService.Infrastructure.Services;

public class ProductService : ICatalogService<Product>
{
    private readonly IRepository<Product> _productRepository;

    public ProductService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<Product?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        return _productRepository.GetItemAsync(id, cancellationToken);
    }

    public Task<IEnumerable<Product>> GetItemsAsync(CancellationToken cancellationToken)
    {
        return _productRepository.GetItemsAsync(cancellationToken);
    }

    public async Task<int> AddItemAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existingProduct = await _productRepository.GetItemAsync(product.Id, cancellationToken);

        if (existingProduct != null)
        {
            throw new InvalidOperationException($"Product with ID {product.Id} already exists.");
        }

        return await _productRepository.AddItemAsync(product, cancellationToken);
    }

    public async Task<int> UpdateItemAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existingProduct = await _productRepository.GetItemAsync(product.Id, cancellationToken);

        if (existingProduct == null)
        {
            throw new InvalidOperationException($"Product with ID {product.Id} does not exist.");
        }

        return await _productRepository.UpdateItemAsync(product, cancellationToken);
    }

    public async Task<int> DeleteItemAsync(int productId, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetItemAsync(productId, cancellationToken);

        if (existingProduct == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} does not exist.");
        }

        return await _productRepository.DeleteItemAsync(productId, cancellationToken);
    }
}