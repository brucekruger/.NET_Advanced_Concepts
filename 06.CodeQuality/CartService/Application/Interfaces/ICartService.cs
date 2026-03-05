using CartService.Domain;

namespace CartService.Application.Interfaces;

public interface ICartService
{
    IEnumerable<Cart> GetAllCarts();
    Cart? GetCart(string cartId);
    Cart CreateCart();
    void UpdateCart(Cart cart);
    CartItem AddItem(string cartId, CartItem item);
    CartItem? UpdateItemQuantity(string cartId, int itemId, int quantity);
    void UpdateItem(CartItem item);
    bool RemoveItem(string cartId, int itemId);
    bool ClearCart(string cartId);
}