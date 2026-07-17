using CartService.Application.Interfaces;
using CartService.Domain;
using LiteDB;
using StackExchange.Redis;
using System.Text.Json;

namespace CartService.Infrastructure.Data;

public class CartRepository : ICartRepository
{
    private readonly LiteDatabase _database;
    private readonly bool _ownsDatabase;
    private readonly IConnectionMultiplexer? _redis;
    private const string CacheKeyPrefix = "cart:";
    private const int CacheTTLSeconds = 3600; // 1 hour

    public CartRepository(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        _ownsDatabase = true;
        _redis = null;
    }

    public CartRepository(string connectionString, IConnectionMultiplexer? redis)
    {
        _database = new LiteDatabase(connectionString);
        _ownsDatabase = true;
        _redis = redis;
    }

    public CartRepository(LiteDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = false;
        _redis = null;
    }

    public CartRepository(LiteDatabase database, IConnectionMultiplexer? redis)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = false;
        _redis = redis;
    }

    private ILiteCollection<Cart> Carts => _database.GetCollection<Cart>("carts");
    private ILiteCollection<CartItem> CartItems => _database.GetCollection<CartItem>("cartItems");

    private string GetCacheKey(string cartId) => $"{CacheKeyPrefix}{cartId}";

    private async Task<Cart?> GetFromCacheAsync(string cartId)
    {
        if (_redis == null || !_redis.IsConnected) return null;

        try
        {
            var db = _redis.GetDatabase();
            var cachedValue = await db.StringGetAsync(GetCacheKey(cartId));
            if (cachedValue.IsNull) return null;

            var cart = System.Text.Json.JsonSerializer.Deserialize<Cart>(cachedValue.ToString());
            return cart;
        }
        catch
        {
            // Gracefully handle cache errors
            return null;
        }
    }

    private async Task SetCacheAsync(Cart cart)
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var db = _redis.GetDatabase();
            var jsonValue = System.Text.Json.JsonSerializer.Serialize(cart);
            await db.StringSetAsync(GetCacheKey(cart.Id), jsonValue, TimeSpan.FromSeconds(CacheTTLSeconds));
        }
        catch
        {
            // Gracefully handle cache errors
        }
    }

    private async Task RemoveCacheAsync(string cartId)
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(GetCacheKey(cartId));
        }
        catch
        {
            // Gracefully handle cache errors
        }
    }

    public IEnumerable<Cart> GetAllCarts()
    {
        return Carts.Include(c => c.CartItems).FindAll().ToArray();
    }

    public Cart? GetCart(string cartId)
    {
        var cart = Carts.FindById(cartId);
        if (cart != null)
        {
            cart.CartItems = CartItems.Find(x => x.CartId == cartId).ToList();
            // Set cache asynchronously without waiting
            _ = SetCacheAsync(cart);
        }
        return cart;
    }

    public Cart CreateCart(Cart cart)
    {
        Carts.Insert(cart);
        return cart;
    }

    public CartItem AddItem(CartItem item)
    {
        if (item.Id > 0 && CartItems.FindById(item.Id) == null)
        {
            var newItem = new CartItem
            {
                Id = item.Id,
                CartId = item.CartId,
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity
            };
            CartItems.Insert(item.Id, newItem);
            return newItem;
        }

        CartItems.Insert(item);
        return item;
    }

    public void UpdateItem(CartItem item)
    {
        CartItems.Update(item);
    }

    public void RemoveItem(int itemId)
    {
        CartItems.Delete(itemId);
    }

    public void RemoveItems(string cartId)
    {
        CartItems.DeleteMany(x => x.CartId == cartId);
    }

    public void UpdateCart(Cart cart)
    {
        Carts.Update(cart);
        // Invalidate cache asynchronously without waiting
        _ = RemoveCacheAsync(cart.Id);
    }

    public void Dispose()
    {
        if (_ownsDatabase)
        {
            _database.Dispose();
        }
    }
}
