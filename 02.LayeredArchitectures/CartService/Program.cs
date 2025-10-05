using CartService.Domain;
using CartService.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace CartService;

internal class Program
{
    public static IConfiguration? Configuration { get; private set; }

    private static void Main(string[] args)
    {
        CartConfiguration.ConfigureMapping();

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json") // This extension method is in Microsoft.Extensions.Configuration.Json
            .Build();

        var connectionString = Configuration.GetConnectionString("LiteDB");

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. List all carts");
            Console.WriteLine("2. Add a new cart");
            Console.WriteLine("3. Delete a cart by Id");
            Console.WriteLine("4. Exit");

            var key = Console.ReadKey(intercept: true).Key;

            using var cartService = new Infrastructure.CartService(new CartRepository(connectionString));

            try
            {
                switch (key)
                {
                    case ConsoleKey.D1:
                        // List all carts
                        Console.WriteLine();
                        Console.WriteLine("=== Card list: ===");

                        var carts = cartService.GetCarts();
                        if (carts.Any())
                        {
                            foreach (var cart in carts)
                            {
                                Console.WriteLine(cart);
                            }
                            Console.WriteLine($"Total carts: {carts.Count()}");
                        }
                        else
                        {
                            Console.WriteLine("No carts found.");
                        }

                        break;

                    case ConsoleKey.D2:
                        // Add a new cart
                        Console.WriteLine();
                        Console.WriteLine("=== Adding a new cart... ===");

                        Console.WriteLine("Enter cart Id:");
                        var Id = Console.ReadLine();

                        Console.WriteLine("Enter cart Name:");
                        var Name = Console.ReadLine();

                        Console.WriteLine("Enter cart Image Url:");
                        var ImageUrl = Console.ReadLine();

                        Console.WriteLine("Enter cart Image AltText:");
                        var ImageAlt = Console.ReadLine();

                        Console.WriteLine("Enter cart Price:");
                        var PriceInput = Console.ReadLine();

                        Console.WriteLine("Enter cart Quantity:");
                        var QuantityInput = Console.ReadLine();

                        var newCart = new Cart
                            {
                            Id = int.TryParse(Id, out var id) ? id : 0,
                            Name = Name,
                            Image = new Image
                            {
                                Url = Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri) ? uri : null,
                                AltText = ImageAlt
                            },
                            Price = decimal.TryParse(PriceInput, out var price) ? price : 0m,
                            Quantity = int.TryParse(QuantityInput, out var quantity) ? quantity : 0
                        };

                        var newCartId = cartService.AddCart(newCart);

                        Console.WriteLine($"New cart with id={newCartId} added!");

                        break;

                    case ConsoleKey.D3:
                        // Delete a cart by Id
                        Console.WriteLine();
                        Console.WriteLine("=== Enter cart Id to delete: ===");

                        if (int.TryParse(Console.ReadLine(), out var cartId))
                        {
                            var deleteResult = cartService.DeleteCart(cartId);
                            Console.WriteLine(deleteResult
                                ? $"Cart with id={cartId} deleted!"
                                : $"Cart with id={cartId} not found!");
                        }
                        else
                        {
                            Console.WriteLine("Invalid cart Id!");
                        }

                        break;

                    case ConsoleKey.Escape:
                    case ConsoleKey.D4:
                        return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}