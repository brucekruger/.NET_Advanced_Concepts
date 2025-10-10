using CartService.Domain;
using LiteDB;

namespace CartService.Infrastructure.Data;

public static class CartConfiguration
{
    public static void ConfigureMapping()
    {
        var mapper = BsonMapper.Global;

        // Configure Cart entity mapping
        mapper.Entity<Cart>()
            .Id(x => x.Id)
            .DbRef(x => x.CartItems, "cartItems");

        // Configure CartItem entity mapping
        mapper.Entity<CartItem>()
            .Id(x => x.Id);

        // Configure Image value object mapping
        mapper.Entity<Image>();
    }

    /// <summary>
    /// Creates necessary indexes for better query performance
    /// </summary>
    /// <param name="database">The LiteDB database instance</param>
    public static void EnsureIndexes(LiteDatabase database)
    {
        // Create index on CartId for faster lookups
        var cartItems = database.GetCollection<CartItem>("cartItems");
        cartItems.EnsureIndex(x => x.CartId);

        // Create index on Cart.Id
        var carts = database.GetCollection<Cart>("carts");
        carts.EnsureIndex(x => x.Id);
    }
}
