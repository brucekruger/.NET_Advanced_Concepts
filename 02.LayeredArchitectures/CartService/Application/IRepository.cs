namespace CartService.Application;

public interface IRepository<T> : IDisposable where T : class
{
    IEnumerable<T> GetItems();
    int AddItem(T item);
    bool DeleteItem(int itemId);
}