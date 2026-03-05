using CartService.Application.Interfaces;
using CartService.Domain;
using LiteDB;

namespace CartService.Infrastructure.Data;

public class CartRepository : ICartRepository
{
    private readonly LiteDatabase _database;
    private readonly bool _ownsDatabase;

    public CartRepository(string connectionString)
    {
        _database = new LiteDatabase(connectionString);
        _ownsDatabase = true;
    }

    public CartRepository(LiteDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _ownsDatabase = false;
    }

    private ILiteCollection<Cart> Carts => _database.GetCollection<Cart>("carts");
    private ILiteCollection<CartItem> CartItems => _database.GetCollection<CartItem>("cartItems");

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
    }

    public void Dispose()
    {
        if (_ownsDatabase)
        {
            _database.Dispose();
        }
    }
}