using LiteDB;

namespace CartService.Domain;

public class Cart
{
    [BsonId]
    public string Id { get; set; } = null!;

    public List<CartItem> CartItems { get; set; } = [];
}