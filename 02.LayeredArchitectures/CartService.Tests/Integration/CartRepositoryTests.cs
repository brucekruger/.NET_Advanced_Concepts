using CartService.Application.Interfaces;
using CartService.Domain;
using CartService.Infrastructure.Data;
using LiteDB;

namespace CartService.Tests.Integration;

[Collection("Database")]
public class CartRepositoryTests : IDisposable
{
    private readonly ICartRepository _repository;
    private readonly LiteDatabase _database;

    public CartRepositoryTests()
    {
        // Use in-memory database for testing
        var memoryStream = new MemoryStream();
        _database = new LiteDatabase(memoryStream);

        CartConfiguration.ConfigureMapping();
        CartConfiguration.EnsureIndexes(_database);

        // Pass special connection string for memory database
        _repository = new CartRepository(_database);
    }

    [Fact]
    public void GetCart_WhenCartExists_ShouldReturnCart()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        _repository.CreateCart(cart);

        // Act
        var result = _repository.GetCart(cart.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cart.Id, result.Id);
    }

    [Fact]
    public void CreateCart_ShouldCreateNewCart()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };

        // Act
        var result = _repository.CreateCart(cart);

        // Assert
        Assert.NotNull(result);
        var savedCart = _repository.GetCart(cart.Id);
        Assert.NotNull(savedCart);
        Assert.Equal(cart.Id, savedCart.Id);
    }

    [Fact]
    public void AddItem_ShouldAddItemToCart()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        _repository.CreateCart(cart);

        var item = new CartItem
        {
            Id = 1,
            Name = "Test Item",
            Price = 10.00m,
            Quantity = 1,
            CartId = cart.Id
        };

        // Act
        var result = _repository.AddItem(item);

        // Assert
        Assert.NotNull(result);
        var savedCart = _repository.GetCart(cart.Id);
        Assert.Contains(savedCart.CartItems, i => i.Id == item.Id);
    }

    [Fact]
    public void UpdateItem_ShouldUpdateExistingItem()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        _repository.CreateCart(cart);

        var item = new CartItem
        {
            Id = 1,
            Name = "Test Item",
            Price = 10.00m,
            Quantity = 1,
            CartId = cart.Id
        };
        _repository.AddItem(item);

        // Act
        item.Quantity = 2;
        _repository.UpdateItem(item);

        // Assert
        var savedCart = _repository.GetCart(cart.Id);
        var updatedItem = savedCart.CartItems.First(i => i.Id == item.Id);
        Assert.Equal(2, updatedItem.Quantity);
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemFromCart()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        _repository.CreateCart(cart);

        var item = new CartItem
        {
            Id = 1,
            Name = "Test Item",
            Price = 10.00m,
            Quantity = 1,
            CartId = cart.Id
        };
        _repository.AddItem(item);

        // Act
        _repository.RemoveItem(item.Id);

        // Assert
        var savedCart = _repository.GetCart(cart.Id);
        Assert.DoesNotContain(savedCart.CartItems, i => i.Id == item.Id);
    }

    [Fact]
    public void RemoveItems_ShouldRemoveAllItemsFromCart()
    {
        // Arrange
        var cart = new Cart { Id = Guid.NewGuid().ToString() };
        _repository.CreateCart(cart);

        for (int i = 1; i <= 3; i++)
        {
            var item = new CartItem
            {
                Id = i,
                Name = $"Test Item {i}",
                Price = 10.00m * i,
                Quantity = i,
                CartId = cart.Id
            };
            _repository.AddItem(item);
        }

        // Act
        _repository.RemoveItems(cart.Id);

        // Assert
        var savedCart = _repository.GetCart(cart.Id);
        Assert.Empty(savedCart.CartItems);
    }

    public void Dispose()
    {
        _repository?.Dispose();
        _database?.Dispose();
    }
}

// Add this collection definition to prevent parallel execution of tests
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<CartRepositoryTests>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}