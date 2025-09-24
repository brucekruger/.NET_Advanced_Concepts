using CartService.Domain;

namespace CartService.Application;

public interface ICartService
{
    IEnumerable<Cart> GetCarts();
    int AddCart(Cart cart);
    bool DeleteCart(int cartId);
}