using AutoFixture;
using CartService.Api.Controllers.V1;
using CartService.Api.Models;
using CartService.Application.Interfaces;
using CartService.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CartService.Tests.Unit;

public class CartV1ControllerTests
{
    private readonly CartController _cartV1Controller;
    private readonly Mock<ICartService> _mockCartService = new();
    private readonly Fixture _fixture;

    public CartV1ControllerTests()
    {
        _cartV1Controller = new CartController(_mockCartService.Object);
        _fixture = new Fixture();
    }

    [Fact]
    public void Ctor_NullCartService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CartController(null!));
    }

    [Fact]
    public void GetCartInfo_ValidId_ReturnsOkResult()
    {
        // Arrange
        const string cartId = "test-cart-id";
        var cart = new Cart { Id = cartId, CartItems = [] };
        _mockCartService.Setup(s => s.GetCart(cartId)).Returns(cart);

        // Act
        var actualResult = _cartV1Controller.GetCartInfo(cartId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actualResult.Result);
        var returnValue = Assert.IsType<CartDto>(okResult.Value);
        Assert.Equal(cartId, returnValue.CartId);
    }

    [Fact]
    public void GetCartInfo_CartNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        const string cartId = "non-existent-cart-id";
        _mockCartService.Setup(s => s.GetCart(cartId)).Returns((Cart?)null);

        // Act
        var actualResult = _cartV1Controller.GetCartInfo(cartId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actualResult.Result);
        Assert.Equal(cartId, notFoundResult.Value);
    }

    [Fact]
    public void GetCartInfo_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        const string cartId = "test-cart-id";
        _mockCartService.Setup(s => s.GetCart(cartId)).Throws(new Exception("Test exception"));
        // Act
        var actualResult = _cartV1Controller.GetCartInfo(cartId);
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actualResult.Result);
        var exception = Assert.IsType<Exception>(badRequestResult.Value);
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void AddItemToCart_ValidCartDto_ReturnsOkResult()
    {
        // Arrange
        const string cartId = "test-cart-id";
        var cartItemDto = _fixture.Create<CartItemDto>();
        var cartDto = new CartDto(cartId, [cartItemDto]);
        var cartItem = new CartItem
        {
            Id = cartItemDto.Id,
            Name = cartItemDto.Name,
            Image = new Image { Url = cartItemDto.Image?.Url, AltText = cartItemDto.Image?.AltText },
            Price = cartItemDto.Price,
            Quantity = cartItemDto.Quantity,
            CartId = cartId
        };
        _mockCartService.Setup(s => s.AddItem(cartId, It.IsAny<CartItem>())).Returns(cartItem);

        // Act
        var actualResult = _cartV1Controller.AddItemToCart(cartDto);

        // Assert
        var okResult = Assert.IsType<CreatedAtActionResult>(actualResult.Result);
        var returnValue = Assert.IsType<CartItem>(okResult.Value);
        Assert.Equal(cartItemDto.Id, returnValue.Id);
        Assert.Equal(cartItemDto.Name, returnValue.Name);
        Assert.Equal(cartItemDto.Price, returnValue.Price);
        Assert.Equal(cartItemDto.Quantity, returnValue.Quantity);
        Assert.Equal(cartId, returnValue.CartId);
    }

    [Fact]
    public void AddItemToCart_EmptyItems_ReturnsBadRequest()
    {
        // Arrange
        const string cartId = "test-cart-id";
        var cartDto = new CartDto(cartId, Array.Empty<CartItemDto>());

        // Act
        var actualResult = _cartV1Controller.AddItemToCart(cartDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actualResult.Result);
        Assert.Equal("Cart must contain at least one item.", badRequestResult.Value);
    }

    [Fact]
    public void AddItemToCart_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        const string cartId = "test-cart-id";
        var cartItemDto = _fixture.Create<CartItemDto>();
        var cartDto = new CartDto(cartId, [cartItemDto]);
        _mockCartService.Setup(s => s.AddItem(cartId, It.IsAny<CartItem>()))
            .Throws(new Exception("Test exception"));

        // Act
        var actualResult = _cartV1Controller.AddItemToCart(cartDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actualResult.Result);
        var exception = Assert.IsType<Exception>(badRequestResult.Value);
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void DeleteItem_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        const string cartId = "test-cart-id";
        const int itemId = 1;
        var cart = new Cart { Id = cartId, CartItems = [new CartItem { Id = itemId, CartId = cartId }] };
        _mockCartService.Setup(s => s.GetCart(cartId)).Returns(cart);
        _mockCartService.Setup(s => s.RemoveItem(cartId, itemId)).Returns(true);

        // Act
        var actualResult = _cartV1Controller.DeleteItem(cartId, itemId);

        // Assert
        Assert.IsType<NoContentResult>(actualResult);
    }

    [Fact]
    public void DeleteItem_NonExistingItem_ReturnsNoContent()
    {
        // Arrange
        const string cartId = "test-cart-id";
        const int itemId = 1;
        var cart = new Cart { Id = cartId, CartItems = [] };
        _mockCartService.Setup(s => s.GetCart(cartId)).Returns(cart);
        _mockCartService.Setup(s => s.RemoveItem(cartId, itemId)).Returns(false);

        // Act
        var actualResult = _cartV1Controller.DeleteItem(cartId, itemId);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(actualResult);
        Assert.Equal(actualResult, noContentResult);
    }

    [Fact]
    public void DeleteItem_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        const string cartId = "test-cart-id";
        const int itemId = 1;
        var cart = new Cart { Id = cartId, CartItems = [new CartItem { Id = itemId, CartId = cartId }] };
        _mockCartService.Setup(s => s.GetCart(cartId)).Returns(cart);
        _mockCartService.Setup(s => s.RemoveItem(cartId, itemId))
            .Throws(new Exception("Test exception"));

        // Act
        var actualResult = _cartV1Controller.DeleteItem(cartId, itemId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actualResult);
        var exception = Assert.IsType<Exception>(badRequestResult.Value);
        Assert.Equal("Test exception", exception.Message);
    }
}