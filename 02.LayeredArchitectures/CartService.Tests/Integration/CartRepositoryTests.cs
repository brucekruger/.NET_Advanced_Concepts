using AutoFixture;
using CartService.Domain;
using CartService.Infrastructure;

namespace CartService.Tests.Integration;

public class CartRepositoryTests
{
    private const string TEST_CONNECTION_STRING = "Filename=:memory:";
    private readonly Fixture _fixture;

    public CartRepositoryTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void GetItems_WhenHasCarts_ShouldReturnAllCarts()
    {
        // Arrange
        using var repo = new CartRepository(TEST_CONNECTION_STRING);
        var expectedCart1 = _fixture.Create<Cart>();
        expectedCart1.Id = 1;
        var expectedCart2 = _fixture.Create<Cart>();
        expectedCart2.Id = 2;

        // Act
        repo.AddItem(expectedCart1);
        repo.AddItem(expectedCart2);
        var actualCarts = repo.GetItems().ToArray();

        // Assert
        Assert.Equal(2, actualCarts.Length);
        Assert.Contains(actualCarts, c => c.Id == expectedCart1.Id);
        Assert.Contains(actualCarts, c => c.Id == expectedCart2.Id);
    }

    [Fact]
    public void AddItem_WhenCorrectData_ShouldAddCartToDatabase()
    {
        // Arrange
        using var repo = new CartRepository(TEST_CONNECTION_STRING);
        var expectedCart = _fixture.Create<Cart>();

        // Act
        var actualId = repo.AddItem(expectedCart);
        var actualCarts = repo.GetItems().ToArray();

        // Assert
        Assert.Single(actualCarts);
        Assert.Equal(expectedCart.Id, actualId);
        Assert.Equal(expectedCart.Id, actualCarts[0].Id);
    }

    [Fact]
    public void DeleteItem_WhenAdded_ShouldRemoveCartFromDatabase()
    {
        // Arrange
        using var repo = new CartRepository(TEST_CONNECTION_STRING);
        var expectedCart = _fixture.Create<Cart>();

        // Act
        repo.AddItem(expectedCart);
        var deletedResult = repo.DeleteItem(expectedCart.Id);

        // Assert
        Assert.True(deletedResult);
        Assert.Empty(repo.GetItems());
    }
}
