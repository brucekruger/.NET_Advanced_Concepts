using CartService.Domain;

namespace CartService.Application.Interfaces;

/// <summary>
/// Repository interface for cart operations
/// </summary>
public interface ICartRepository : IDisposable
{
    /// <summary>
    /// Gets a cart by its ID
    /// </summary>
    /// <param name="cartId">The unique identifier of the cart</param>
    /// <returns>The cart if found, null otherwise</returns>
    Cart? GetCart(string cartId);

    /// <summary>
    /// Creates a new cart
    /// </summary>
    /// <param name="cart">The cart to create</param>
    /// <returns>The created cart</returns>
    Cart CreateCart(Cart cart);

    /// <summary>
    /// Adds a new item to a cart
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns>The added cart item</returns>
    CartItem AddItem(CartItem item);

    /// <summary>
    /// Updates an existing cart item
    /// </summary>
    /// <param name="item">The item to update</param>
    void UpdateItem(CartItem item);

    /// <summary>
    /// Removes a specific item from a cart
    /// </summary>
    /// <param name="itemId">The ID of the item to remove</param>
    void RemoveItem(int itemId);

    /// <summary>
    /// Removes all items from a specific cart
    /// </summary>
    /// <param name="cartId">The ID of the cart to clear</param>
    void RemoveItems(string cartId);

    /// <summary>
    /// Updates the cart entity
    /// </summary>
    /// <param name="cart">The cart to update</param>
    void UpdateCart(Cart cart);
}