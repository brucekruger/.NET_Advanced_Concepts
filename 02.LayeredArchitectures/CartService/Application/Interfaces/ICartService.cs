using CartService.Domain;

namespace CartService.Application.Interfaces;

public interface ICartService
{
    Cart? GetCart(string cartId);
    Cart CreateCart();
    CartItem AddItem(string cartId, CartItem item);
    CartItem? UpdateItemQuantity(string cartId, int itemId, int quantity);
    bool RemoveItem(string cartId, int itemId);
    bool ClearCart(string cartId);
}