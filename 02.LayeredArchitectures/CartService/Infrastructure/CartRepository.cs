using CartService.Application;
using CartService.Domain;
using LiteDB;

namespace CartService.Infrastructure;

public sealed class CartRepository : IRepository<Cart>
{
    private const string CollectionName = "carts";
    private readonly LiteDatabase _database;

    public CartRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _database = new LiteDatabase(connectionString);
    }

    public IEnumerable<Cart> GetItems()
    {
        var carts = _database.GetCollection<Cart>(CollectionName);
        return carts.FindAll();
    }

    public int AddItem(Cart item)
    {
        var carts = _database.GetCollection<Cart>(CollectionName);
        return (int)carts.Insert(item);
    }

    public bool DeleteItem(int itemId)
    {
        var carts = _database.GetCollection<Cart>(CollectionName);
        return carts.Delete(itemId);
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}
