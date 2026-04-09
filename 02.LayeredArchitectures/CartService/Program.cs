using CartService.Application.Interfaces;
using CartService.Domain;
using CartService.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CartService;

internal class Program
{
    public static IConfiguration? Configuration { get; private set; }

    private static void Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = Configuration.GetConnectionString("LiteDB")
            ?? throw new InvalidOperationException("LiteDB connection string not found in configuration.");

        // Ensure data directory exists
        var dbDirectory = Path.GetDirectoryName(connectionString.Replace("Filename=", string.Empty));
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Configure LiteDB
        CartConfiguration.ConfigureMapping();

        // Setup DI
        var services = new ServiceCollection();
        services.AddScoped<ICartRepository>(_ => new CartRepository(connectionString));
        services.AddScoped<ICartService, Infrastructure.Services.CartService>();
        var serviceProvider = services.BuildServiceProvider();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Cart Management System ===");
            Console.WriteLine("1. List all carts");
            Console.WriteLine("2. Create new cart");
            Console.WriteLine("3. View cart details");
            Console.WriteLine("4. Add item to cart");
            Console.WriteLine("5. Update item quantity");
            Console.WriteLine("6. Remove item from cart");
            Console.WriteLine("7. Clear cart");
            Console.WriteLine("0. Exit");
            Console.Write("\nSelect an option: ");

            var key = Console.ReadKey(intercept: true).Key;
            Console.WriteLine();

            try
            {
                using var scope = serviceProvider.CreateScope();
                var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

                switch (key)
                {
                    case ConsoleKey.D1:
                        ListAllCarts(cartService);
                        break;

                    case ConsoleKey.D2:
                        CreateNewCart(cartService);
                        break;

                    case ConsoleKey.D3:
                        ViewCartDetails(cartService);
                        break;

                    case ConsoleKey.D4:
                        AddItemToCart(cartService);
                        break;

                    case ConsoleKey.D5:
                        UpdateItemQuantity(cartService);
                        break;

                    case ConsoleKey.D6:
                        RemoveItemFromCart(cartService);
                        break;

                    case ConsoleKey.D7:
                        ClearCart(cartService);
                        break;

                    case ConsoleKey.D0:
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nError: {e.Message}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey(intercept: true);
        }
    }

    private static void ListAllCarts(ICartService cartService)
    {
        Console.WriteLine("\n=== All Carts ===");
        var cart = cartService.CreateCart(); // Temporary for testing
        Console.WriteLine($"Cart created with ID: {cart.Id}");
    }

    private static void CreateNewCart(ICartService cartService)
    {
        Console.WriteLine("\n=== Create New Cart ===");
        var cart = cartService.CreateCart();
        Console.WriteLine($"New cart created with ID: {cart.Id}");
    }

    private static void ViewCartDetails(ICartService cartService)
    {
        Console.Write("\nEnter cart ID: ");
        var cartId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cartId))
        {
            Console.WriteLine("Invalid cart ID!");
            return;
        }

        var cart = cartService.GetCart(cartId);
        if (cart == null)
        {
            Console.WriteLine("Cart not found!");
            return;
        }

        Console.WriteLine($"\n=== Cart {cart.Id} ===");
        foreach (var item in cart.CartItems)
        {
            Console.WriteLine(item);
        }
    }

    private static void AddItemToCart(ICartService cartService)
    {
        Console.Write("\nEnter cart ID: ");
        var cartId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cartId))
        {
            Console.WriteLine("Invalid cart ID!");
            return;
        }

        Console.Write("Enter item name: ");
        var name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Name is required!");
            return;
        }

        Console.Write("Enter price: ");
        if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
        {
            Console.WriteLine("Invalid price!");
            return;
        }

        Console.Write("Enter quantity: ");
        if (!int.TryParse(Console.ReadLine(), out var quantity) || quantity <= 0)
        {
            Console.WriteLine("Invalid quantity!");
            return;
        }

        var item = new CartItem
        {
            Id = Random.Shared.Next(1, 10000), // Simple ID generation for demo
            Name = name,
            Price = price,
            Quantity = quantity
        };

        var addedItem = cartService.AddItem(cartId, item);
        Console.WriteLine($"\nItem added: {addedItem.Name}");
    }

    private static void UpdateItemQuantity(ICartService cartService)
    {
        Console.Write("\nEnter cart ID: ");
        var cartId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cartId))
        {
            Console.WriteLine("Invalid cart ID!");
            return;
        }

        Console.Write("Enter item ID: ");
        if (!int.TryParse(Console.ReadLine(), out var itemId))
        {
            Console.WriteLine("Invalid item ID!");
            return;
        }

        Console.Write("Enter new quantity: ");
        if (!int.TryParse(Console.ReadLine(), out var quantity))
        {
            Console.WriteLine("Invalid quantity!");
            return;
        }

        var updatedItem = cartService.UpdateItemQuantity(cartId, itemId, quantity);
        if (updatedItem != null)
        {
            Console.WriteLine($"Item quantity updated to: {updatedItem.Quantity}");
        }
        else
        {
            Console.WriteLine("Item removed from cart (quantity was 0) or not found");
        }
    }

    private static void RemoveItemFromCart(ICartService cartService)
    {
        Console.Write("\nEnter cart ID: ");
        var cartId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cartId))
        {
            Console.WriteLine("Invalid cart ID!");
            return;
        }

        Console.Write("Enter item ID: ");
        if (!int.TryParse(Console.ReadLine(), out var itemId))
        {
            Console.WriteLine("Invalid item ID!");
            return;
        }

        var result = cartService.RemoveItem(cartId, itemId);
        Console.WriteLine(result ? "Item removed successfully!" : "Item not found!");
    }

    private static void ClearCart(ICartService cartService)
    {
        Console.Write("\nEnter cart ID: ");
        var cartId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(cartId))
        {
            Console.WriteLine("Invalid cart ID!");
            return;
        }

        var result = cartService.ClearCart(cartId);
        Console.WriteLine(result ? "Cart cleared successfully!" : "Cart not found!");
    }
}