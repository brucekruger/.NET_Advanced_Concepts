using AutoFixture;
using CartService.Application;
using CartService.Domain;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace CartService.Tests.Unit;

public class CartServiceTests
{
    private readonly Mock<IRepository<Cart>> _cartRepositoryMock;
    private readonly ICartService _cartService;
    private readonly Fixture _fixture;

    public CartServiceTests()
    {
        _fixture = new Fixture();
        _cartRepositoryMock = new Mock<IRepository<Cart>>();
        _cartService = new Infrastructure.CartService(_cartRepositoryMock.Object);
    }

    [Fact]
    public void GetCarts_WhenHasCarts_ShouldReturnCarts()
    {
        // Arrange
        Cart[] expectedCarts = [_fixture.Create<Cart>(), _fixture.Create<Cart>(), _fixture.Create<Cart>()];
        _cartRepositoryMock.Reset();
        _cartRepositoryMock.Setup(x => x.GetItems())
            .Returns(() => expectedCarts);

        // Act
        var actualCarts = _cartService.GetCarts().ToArray();

        // Assert
        _cartRepositoryMock.Verify(x => x.GetItems(), Times.Once);
        Assert.NotEmpty(actualCarts);
        Assert.Equal(expectedCarts.Length, actualCarts.Length);
    }

    [Fact]
    public void AddCart_WhenCorrectData_ShouldExecuteAddCart()
    {
        // Arrange
        var expectedCart = _fixture.Create<Cart>();
        _cartRepositoryMock.Reset();
        _cartRepositoryMock.Setup(x => x.AddItem(It.IsAny<Cart>()))
            .Returns(() => expectedCart.Id);

        // Act
        var actualId = _cartService.AddCart(expectedCart);

        // Assert
        _cartRepositoryMock.Verify(x => x.AddItem(It.IsAny<Cart>()), Times.Once);
        Assert.Equal(expectedCart.Id, actualId);
    }

    [Fact]
    public void AddCart_WhenCartIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        _cartRepositoryMock.Reset();
        _cartRepositoryMock.Setup(x => x.AddItem(It.IsAny<Cart>()))
            .Returns(It.IsAny<int>());

        // Act - Assert
        Assert.Throws<ArgumentNullException>(() => _cartService.AddCart(null));
        _cartRepositoryMock.Verify(x => x.AddItem(It.IsAny<Cart>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(GetInvalidCarts))]
    public void AddCart_WhenMissingData_ShouldThrowValidationException(Cart cart)
    {
        // Arrange
        _cartRepositoryMock.Reset();
        _cartRepositoryMock.Setup(x => x.AddItem(It.IsAny<Cart>()))
            .Returns(It.IsAny<int>());

        // Act - Assert
        Assert.Throws<ValidationException>(() => _cartService.AddCart(cart));
        _cartRepositoryMock.Verify(x => x.AddItem(It.IsAny<Cart>()), Times.Never);
    }

    public static IEnumerable<object[]> GetInvalidCarts()
    {
        yield return
        [
            new Cart
            {
                Id = 0,  // Invalid: Id is 0
                Name = "Test Cart",
                Image = new Image
                {
                    Url = new Uri("https://example.com/image.png"),
                    AltText = "Test image"
                },
                Price = 100,
                Quantity = 1
            }
        ];

        yield return
        [
            new Cart
            {
                Id = 1,
                Name = string.Empty,  // Invalid: Name is empty
                Image = new Image
                {
                    Url = new Uri("https://example.com/image2.png"),
                    AltText = "Test image 2"
                },
                Price = 200,
                Quantity = 2
            }
        ];

        yield return
        [
            new Cart
            {
                Id = 2,
                Name = "Test Cart",
                Image = new Image
                {
                    Url = new Uri("https://example.com/image2.png"),
                    AltText = "Test image 2"
                },
                Price = -100,  // Invalid: Price is -100
                Quantity = 2
            }
        ];
    }
}