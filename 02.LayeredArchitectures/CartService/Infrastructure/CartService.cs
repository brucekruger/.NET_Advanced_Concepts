using CartService.Application;
using CartService.Domain;
using System.ComponentModel.DataAnnotations;

namespace CartService.Infrastructure;

public class CartService : ICartService, IDisposable
{
    private readonly IRepository<Cart> _cartRepository;

    public CartService(IRepository<Cart> cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public IEnumerable<Cart> GetCarts()
    {
        var carts = _cartRepository.GetItems();
        return carts;
    }

    public int AddCart(Cart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (!TryValidateCart(cart, out var results))
        {
            var errors = string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Cart validation failed: {Environment.NewLine}{errors}");
        }

        var id = _cartRepository.AddItem(cart);
        return id;
    }

    public bool DeleteCart(int cartId)
    {
        var result = _cartRepository.DeleteItem(cartId);
        return result;
    }

    private static bool TryValidateCart(Cart cart, out List<ValidationResult> results)
    {
        var context = new ValidationContext(cart);
        results = [];
        return Validator.TryValidateObject(cart, context, results, validateAllProperties: true);
    }

    public void Dispose()
    {
        _cartRepository.Dispose();
    }
}