using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Messaging;

namespace CatalogService.Infrastructure.Services;

public class ProductService : ICatalogService<Product>
{
    private readonly IRepository<Product> _productRepository;
    private readonly ProductEventPublisher _eventPublisher;

    public ProductService(IRepository<Product> productRepository, ProductEventPublisher eventPublisher)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
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

        var result = await _productRepository.AddItemAsync(product, cancellationToken);
        
        // Publish product created event
        if (result > 0)
        {
            await _eventPublisher.PublishProductCreatedAsync(product, cancellationToken);
        }

        return result;
    }

    public async Task<int> UpdateItemAsync(Product product, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existingProduct = await _productRepository.GetItemAsync(product.Id, cancellationToken);

        if (existingProduct == null)
        {
            throw new InvalidOperationException($"Product with ID {product.Id} does not exist.");
        }

        var result = await _productRepository.UpdateItemAsync(product, cancellationToken);

        // Publish product updated event
        if (result > 0)
        {
            await _eventPublisher.PublishProductUpdatedAsync(product, cancellationToken);
        }

        return result;
    }

    public async Task<int> DeleteItemAsync(int productId, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetItemAsync(productId, cancellationToken);

        if (existingProduct == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} does not exist.");
        }

        var result = await _productRepository.DeleteItemAsync(productId, cancellationToken);

        // Publish product deleted event
        if (result > 0)
        {
            await _eventPublisher.PublishProductDeletedAsync(productId, cancellationToken);
        }

        return result;
    }
}