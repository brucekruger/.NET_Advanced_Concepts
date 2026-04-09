using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using CatalogService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogService;

internal class Program
{
    public static IConfiguration? Configuration { get; private set; }

    private static async Task Main(string[] args)
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json") // This extension method is in Microsoft.Extensions.Configuration.Json
            .Build();

        var connectionString = Configuration.GetConnectionString("MSSQL");

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        serviceCollection.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        serviceCollection.AddScoped<IRepository<Category>, CategoryRepository>();
        serviceCollection.AddScoped<IRepository<Product>, ProductRepository>();
        serviceCollection.AddScoped<ICatalogService<Category>, CategoryService>();
        serviceCollection.AddScoped<ICatalogService<Product>, ProductService>();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        var categoryService = serviceProvider.GetService(typeof(ICatalogService<Category>)) as ICatalogService<Category>;
        var productService = serviceProvider.GetService(typeof(ICatalogService<Product>)) as ICatalogService<Product>;

        if (dbContext != null)
        {
            var cancellationToken = new CancellationToken();

            //await dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            Console.WriteLine("Database loaded.");
            if (categoryService != null && productService != null)
            {
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("=== Catalog Management System ===");
                    Console.WriteLine("1. Categories");
                    Console.WriteLine("2. Products");
                    Console.WriteLine("0. Exit");
                    Console.Write("\nSelect an option: ");

                    var choice = Console.ReadLine();

                    try
                    {
                        switch (choice)
                        {
                            case "1":
                                await ManageCategoriesMenu(categoryService, cancellationToken);
                                break;
                            case "2":
                                await ManageProductsMenu(productService, categoryService, cancellationToken);
                                break;
                            case "0":
                                return;
                            default:
                                Console.WriteLine("\nInvalid option. Press any key to continue...");
                                Console.ReadKey();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        throw;
                    }
                }
            }
            Console.WriteLine("Services are not available.");
        }
        else
        {
            Console.WriteLine("Can't load database!");
        }
    }

    private static async Task ManageCategoriesMenu(ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Category Management ===");
            Console.WriteLine("1. List all categories");
            Console.WriteLine("2. Add new category");
            Console.WriteLine("3. Edit category");
            Console.WriteLine("4. Delete category");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await ListCategories(categoryService, cancellationToken);
                    break;
                case "2":
                    await AddCategory(categoryService, cancellationToken);
                    break;
                case "3":
                    await EditCategory(categoryService, cancellationToken);
                    break;
                case "4":
                    await DeleteCategory(categoryService, cancellationToken);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("\nInvalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static async Task ManageProductsMenu(ICatalogService<Product> productService, ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Product Management ===");
            Console.WriteLine("1. List all products");
            Console.WriteLine("2. Add new product");
            Console.WriteLine("3. Edit product");
            Console.WriteLine("4. Delete product");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await ListProducts(productService, cancellationToken);
                    break;
                case "2":
                    await AddProduct(productService, categoryService, cancellationToken);
                    break;
                case "3":
                    await EditProduct(productService, categoryService, cancellationToken);
                    break;
                case "4":
                    await DeleteProduct(productService, cancellationToken);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("\nInvalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static async Task ListCategories(ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Categories List ===\n");

        var categories = await categoryService.GetItemsAsync(cancellationToken);
        foreach (var category in categories)
        {
            Console.Write(category.ToString());
            Console.WriteLine();
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private static async Task AddCategory(ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Add New Category ===\n");

        Console.Write("Enter category name: ");
        var name = Console.ReadLine();

        Console.Write("Enter image URL (optional, press Enter to skip): ");
        var imageUrl = Console.ReadLine();

        Console.Write("Enter parent category ID (optional, press Enter to skip): ");
        var parentIdInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("\nCategory name is required!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var category = new Category { Name = name };

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                category.Image = uri;
            }
        }

        if (!string.IsNullOrWhiteSpace(parentIdInput) && int.TryParse(parentIdInput, out var parentId))
        {
            var parentCategory = await categoryService.GetItemAsync(parentId, cancellationToken);
            if (parentCategory != null)
            {
                category.Parent = parentCategory;
            }
        }

        await categoryService.AddItemAsync(category, cancellationToken);
        Console.WriteLine("\nCategory added successfully!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task EditCategory(ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Edit Category ===\n");

        Console.Write("Enter category ID to edit: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("\nInvalid ID format!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var category = await categoryService.GetItemAsync(id, cancellationToken);
        if (category == null)
        {
            Console.WriteLine("\nCategory not found!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nCurrent values:");
        Console.WriteLine(category.ToString());
        Console.WriteLine("\nEnter new values (press Enter to keep current value):\n");

        Console.Write("Enter category name: ");
        var name = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(name))
        {
            category.Name = name;
        }

        Console.Write("Enter image URL: ");
        var imageUrl = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            category.Image = uri;
        }

        Console.Write("Enter parent category ID (0 to remove parent): ");
        var parentIdInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(parentIdInput) && int.TryParse(parentIdInput, out var parentId))
        {
            if (parentId == 0)
            {
                category.ParentId = null;
                category.Parent = null;
            }
            else if (parentId != category.Id) // Prevent self-referencing
            {
                var parentCategory = await categoryService.GetItemAsync(parentId, cancellationToken);
                if (parentCategory != null)
                {
                    category.ParentId = parentId;
                    category.Parent = parentCategory;
                }
            }
        }

        await categoryService.UpdateItemAsync(category, cancellationToken);
        Console.WriteLine("\nCategory updated successfully!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task DeleteCategory(ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Delete Category ===\n");

        Console.Write("Enter category ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out var id))
        {
            var category = await categoryService.GetItemAsync(id, cancellationToken);
            if (category != null)
            {
                await categoryService.DeleteItemAsync(id, cancellationToken);
                Console.WriteLine("\nCategory deleted successfully!");
            }
            else
            {
                Console.WriteLine("\nCategory not found!");
            }
        }
        else
        {
            Console.WriteLine("\nInvalid ID format!");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task ListProducts(ICatalogService<Product> productService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Products List ===\n");

        var products = await productService.GetItemsAsync(cancellationToken);
        foreach (var product in products)
        {
            Console.Write(product.ToString());
            Console.WriteLine();
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    private static async Task AddProduct(ICatalogService<Product> productService, ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Add New Product ===\n");

        Console.Write("Enter product name: ");
        var name = Console.ReadLine();

        Console.Write("Enter product description: ");
        var description = Console.ReadLine();

        Console.Write("Enter price: ");
        var priceInput = Console.ReadLine();

        Console.Write("Enter amount: ");
        var amountInput = Console.ReadLine();

        Console.Write("Enter image URL (optional, press Enter to skip): ");
        var imageUrl = Console.ReadLine();

        Console.Write("Enter category ID: ");
        var categoryIdInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            Console.WriteLine("\nName and description are required!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        if (!decimal.TryParse(priceInput, out var price) || !int.TryParse(amountInput, out var amount))
        {
            Console.WriteLine("\nInvalid price or amount format!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        if (!int.TryParse(categoryIdInput, out var categoryId))
        {
            Console.WriteLine("\nInvalid category ID format!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var category = await categoryService.GetItemAsync(categoryId, cancellationToken);
        if (category == null)
        {
            Console.WriteLine("\nCategory not found!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Amount = amount,
            CategoryId = categoryId
        };

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                product.Image = uri;
            }
        }

        await productService.AddItemAsync(product, cancellationToken);
        Console.WriteLine("\nProduct added successfully!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task EditProduct(ICatalogService<Product> productService, ICatalogService<Category> categoryService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Edit Product ===\n");

        Console.Write("Enter product ID to edit: ");
        if (!int.TryParse(Console.ReadLine(), out var id))
        {
            Console.WriteLine("\nInvalid ID format!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var product = await productService.GetItemAsync(id, cancellationToken);
        if (product == null)
        {
            Console.WriteLine("\nProduct not found!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nCurrent values:");
        Console.WriteLine(product.ToString());
        Console.WriteLine("\nEnter new values (press Enter to keep current value):\n");

        Console.Write("Enter product name: ");
        var name = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(name))
        {
            product.Name = name;
        }

        Console.Write("Enter product description: ");
        var description = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(description))
        {
            product.Description = description;
        }

        Console.Write("Enter price: ");
        var priceInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(priceInput) && decimal.TryParse(priceInput, out var price))
        {
            product.Price = price;
        }

        Console.Write("Enter amount: ");
        var amountInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(amountInput) && int.TryParse(amountInput, out var amount) && amount > 0)
        {
            product.Amount = amount;
        }

        Console.Write("Enter image URL: ");
        var imageUrl = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(imageUrl) && Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            product.Image = uri;
        }

        Console.Write("Enter category ID: ");
        var categoryIdInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(categoryIdInput) && int.TryParse(categoryIdInput, out var categoryId))
        {
            var category = await categoryService.GetItemAsync(categoryId, cancellationToken);
            if (category != null)
            {
                product.CategoryId = categoryId;
                product.Category = category;
            }
        }

        await productService.UpdateItemAsync(product, cancellationToken);
        Console.WriteLine("\nProduct updated successfully!");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static async Task DeleteProduct(ICatalogService<Product> productService, CancellationToken cancellationToken)
    {
        Console.Clear();
        Console.WriteLine("=== Delete Product ===\n");

        Console.Write("Enter product ID to delete: ");
        if (int.TryParse(Console.ReadLine(), out var id))
        {
            var product = await productService.GetItemAsync(id, cancellationToken);
            if (product != null)
            {
                await productService.DeleteItemAsync(id, cancellationToken);
                Console.WriteLine("\nProduct deleted successfully!");
            }
            else
            {
                Console.WriteLine("\nProduct not found!");
            }
        }
        else
        {
            Console.WriteLine("\nInvalid ID format!");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}