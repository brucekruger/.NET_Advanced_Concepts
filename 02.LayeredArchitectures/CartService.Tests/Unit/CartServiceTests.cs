using AutoFixture;
using CartService.Application.Interfaces;
using CartService.Domain;
using Moq;

namespace CartService.Tests.Unit;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly ICartService _cartService;
    private readonly Fixture _fixture;

    public CartServiceTests()
    {
        _fixture = new Fixture();
        _cartRepositoryMock = new Mock<ICartRepository>();
        _cartService = new Infrastructure.Services.CartService(_cartRepositoryMock.Object);

        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void GetCart_WhenCartExists_ShouldReturnCart()
    {
        // Arrange
        var expectedCart = _fixture.Create<Cart>();
        var cartId = expectedCart.Id;

        _cartRepositoryMock.Setup(x => x.GetCart(cartId))
            .Returns(expectedCart);

        // Act
        var actualCart = _cartService.GetCart(cartId);

        // Assert
        Assert.NotNull(actualCart);
        Assert.Equal(expectedCart.Id, actualCart.Id);
        _cartRepositoryMock.Verify(x => x.GetCart(cartId), Times.Once);
    }

    [Fact]
    public void CreateCart_ShouldCreateNewCart()
    {
        // Arrange
        Cart? savedCart = null;
        _cartRepositoryMock.Setup(x => x.CreateCart(It.IsAny<Cart>()))
            .Callback<Cart>(cart => savedCart = cart)
            .Returns<Cart>(cart => cart);

        // Act
        var actualResult = _cartService.CreateCart();

        // Assert
        Assert.NotNull(actualResult);
        Assert.NotEmpty(actualResult.Id);
        Assert.Empty(actualResult.CartItems);
        _cartRepositoryMock.Verify(x => x.CreateCart(It.IsAny<Cart>()), Times.Once);
    }

    [Fact]
    public void AddItem_WhenCartExists_ShouldAddNewItem()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        var item = _fixture.Create<CartItem>();

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);
        _cartRepositoryMock.Setup(x => x.AddItem(It.IsAny<CartItem>()))
            .Returns<CartItem>(i => i);

        // Act
        var actualResult = _cartService.AddItem(cart.Id, item);

        // Assert
        Assert.NotNull(actualResult);
        Assert.Equal(item.Id, actualResult.Id);
        Assert.Equal(cart.Id, actualResult.CartId);
        _cartRepositoryMock.Verify(x => x.AddItem(It.IsAny<CartItem>()), Times.Once);
        _cartRepositoryMock.Verify(x => x.UpdateCart(cart), Times.Once);
    }

    [Fact]
    public void AddItem_WhenItemExists_ShouldUpdateQuantity()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        var existingItem = _fixture.Create<CartItem>();
        existingItem.CartId = cart.Id;
        existingItem.Quantity = 1;
        cart.CartItems.Add(existingItem);

        var newItem = new CartItem
        {
            Id = existingItem.Id,
            Quantity = 2
        };

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);

        // Act
        var actualResult = _cartService.AddItem(cart.Id, newItem);

        // Assert
        Assert.Equal(3, actualResult.Quantity);
        _cartRepositoryMock.Verify(x => x.UpdateItem(It.Is<CartItem>(i => i.Quantity == 3)), Times.Once);
    }

    [Fact]
    public void UpdateItemQuantity_WhenValidQuantity_ShouldUpdateItem()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        var item = _fixture.Create<CartItem>();
        item.CartId = cart.Id;
        cart.CartItems.Add(item);
        const int newQuantity = 5;

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);

        // Act
        var actualResult = _cartService.UpdateItemQuantity(cart.Id, item.Id, newQuantity);

        // Assert
        Assert.NotNull(actualResult);
        Assert.Equal(newQuantity, actualResult.Quantity);
        _cartRepositoryMock.Verify(x => x.UpdateItem(It.Is<CartItem>(i => i.Quantity == newQuantity)), Times.Once);
    }

    [Fact]
    public void UpdateItemQuantity_WhenZeroQuantity_ShouldRemoveItem()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        var item = _fixture.Create<CartItem>();
        item.CartId = cart.Id;
        cart.CartItems.Add(item);

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);

        // Act
        var actualResult = _cartService.UpdateItemQuantity(cart.Id, item.Id, 0);

        // Assert
        Assert.Null(actualResult);
        _cartRepositoryMock.Verify(x => x.RemoveItem(item.Id), Times.Once);
        _cartRepositoryMock.Verify(x => x.UpdateCart(cart), Times.Once);
    }

    [Fact]
    public void RemoveItem_WhenItemExists_ShouldRemoveItem()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        var item = _fixture.Create<CartItem>();
        item.CartId = cart.Id;
        cart.CartItems.Add(item);

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);

        // Act
        var actualResult = _cartService.RemoveItem(cart.Id, item.Id);

        // Assert
        Assert.True(actualResult);
        _cartRepositoryMock.Verify(x => x.RemoveItem(item.Id), Times.Once);
        _cartRepositoryMock.Verify(x => x.UpdateCart(cart), Times.Once);
    }

    [Fact]
    public void ClearCart_WhenCartExists_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = _fixture.Create<Cart>();
        cart.CartItems.AddRange(_fixture.CreateMany<CartItem>(3));

        _cartRepositoryMock.Setup(x => x.GetCart(cart.Id))
            .Returns(cart);

        // Act
        var actualResult = _cartService.ClearCart(cart.Id);

        // Assert
        Assert.True(actualResult);
        _cartRepositoryMock.Verify(x => x.RemoveItems(cart.Id), Times.Once);
        _cartRepositoryMock.Verify(x => x.UpdateCart(cart), Times.Once);
    }

    [Fact]
    public void GetCart_WhenCartDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        const string cartId = "non-existent-id";
        _cartRepositoryMock.Setup(x => x.GetCart(cartId))
            .Returns((Cart?)null);

        // Act
        var actualResult = _cartService.GetCart(cartId);

        // Assert
        Assert.Null(actualResult);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetCart_WhenCartIdIsInvalid_ShouldThrowArgumentException(string? cartId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _cartService.GetCart(cartId));
    }
}