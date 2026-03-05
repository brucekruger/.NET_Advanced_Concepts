namespace CatalogService.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetItemAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<T>> GetItemsAsync(CancellationToken cancellationToken);
    Task<int> AddItemAsync(T item, CancellationToken cancellationToken);
    Task<int> UpdateItemAsync(T item, CancellationToken cancellationToken);
    Task<int> DeleteItemAsync(int itemId, CancellationToken cancellationToken);
}