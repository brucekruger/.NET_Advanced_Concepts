using CartService.Application.Interfaces;
using CartService.Domain;

namespace CartService.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;

    public CartService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public Cart? GetCart(string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("Cart ID cannot be null or empty.", nameof(cartId));
        }

        var cart = _cartRepository.GetCart(cartId);
        return cart;
    }

    public Cart CreateCart()
    {
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        var createdCart = _cartRepository.CreateCart(cart);
        return createdCart;
    }

    public CartItem AddItem(string cartId, CartItem item)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("Cart ID cannot be null or empty.", nameof(cartId));
        }

        ArgumentNullException.ThrowIfNull(item);

        var cart = GetCart(cartId) 
            ?? throw new InvalidOperationException($"Cart with ID {cartId} not found");

        var existingItem = cart.CartItems.FirstOrDefault(i => i.Id == item.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
            _cartRepository.UpdateItem(existingItem);
            return existingItem;
        }

        item.CartId = cartId;
        var addedItem = _cartRepository.AddItem(item);
        cart.CartItems.Add(addedItem);
        _cartRepository.UpdateCart(cart);
        
        return addedItem;
    }

    public CartItem? UpdateItemQuantity(string cartId, int itemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("Cart ID cannot be null or empty.", nameof(cartId));
        }

        if (itemId <= 0)
        {
            throw new ArgumentException("Item ID must be greater than zero.", nameof(itemId));
        }

        var cart = GetCart(cartId)
            ?? throw new InvalidOperationException($"Cart with ID {cartId} not found");

        var item = cart.CartItems.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return null;

        if (quantity <= 0)
        {
            cart.CartItems.Remove(item);
            _cartRepository.RemoveItem(item.Id);
        }
        else
        {
            item.Quantity = quantity;
            _cartRepository.UpdateItem(item);
        }

        _cartRepository.UpdateCart(cart);
        return quantity <= 0 ? null : item;
    }

    public bool RemoveItem(string cartId, int itemId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("Cart ID cannot be null or empty.", nameof(cartId));
        }

        if (itemId <= 0)
        {
            throw new ArgumentException("Item ID must be greater than zero.", nameof(itemId));
        }

        var cart = GetCart(cartId)
            ?? throw new InvalidOperationException($"Cart with ID {cartId} not found");

        var item = cart.CartItems.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return false;

        cart.CartItems.Remove(item);
        _cartRepository.RemoveItem(itemId);
        _cartRepository.UpdateCart(cart);
        
        return true;
    }

    public bool ClearCart(string cartId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("Cart ID cannot be null or empty.", nameof(cartId));
        }

        var cart = GetCart(cartId)
            ?? throw new InvalidOperationException($"Cart with ID {cartId} not found");

        _cartRepository.RemoveItems(cart.Id);
        cart.CartItems.Clear();
        _cartRepository.UpdateCart(cart);
        
        return true;
    }
}