using AutoFixture;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Services;
using Moq;

namespace CatalogService.UnitTests;

public class ProductServiceTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly ICatalogService<Product> _productService;
    private readonly Fixture _fixture;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _productService = new ProductService(_productRepositoryMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetItemsAsync_WhenHasProducts_ShouldReturnAllProductsAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var expectedProducts = _fixture.CreateMany<Product>(3).ToArray();
        _productRepositoryMock.Setup(x => x.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProducts);

        // Act
        var actualProducts = (await _productService.GetItemsAsync(CancellationToken.None)).ToArray();

        // Assert
        Assert.NotEmpty(actualProducts);
        Assert.Equal(expectedProducts.Length, actualProducts.Length);
        _productRepositoryMock.Verify(x => x.GetItemsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_WhenProductExists_ShouldReturnProductAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var expectedProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(expectedProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var actualProduct = await _productService.GetItemAsync(expectedProduct.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(actualProduct);
        Assert.Equal(expectedProduct, actualProduct);
        _productRepositoryMock.Verify(x => x.GetItemAsync(expectedProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_WhenProductDoesNotExist_ShouldReturnNullAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        const int expectedProductId = 1;
        _productRepositoryMock.Setup(x => x.GetItemAsync(expectedProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var actualProduct = await _productService.GetItemAsync(expectedProductId, CancellationToken.None);

        // Assert
        Assert.Null(actualProduct);
        _productRepositoryMock.Verify(x => x.GetItemAsync(expectedProductId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductIsValid_ShouldAddProductAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var newProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(newProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepositoryMock.Setup(x => x.AddItemAsync(newProduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var actualResult = await _productService.AddItemAsync(newProduct, CancellationToken.None);

        // Assert
        Assert.Equal(1, actualResult);
        _productRepositoryMock.Verify(x => x.GetItemAsync(newProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.AddItemAsync(newProduct, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        // Arrange
        Product? newProduct = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _productService.AddItemAsync(newProduct!, CancellationToken.None));
        _productRepositoryMock.Verify(x => x.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _productRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductAlreadyExists_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var existingProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _productService.AddItemAsync(existingProduct, CancellationToken.None));
        _productRepositoryMock.Verify(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenProductIsValid_ShouldUpdateProductAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var existingProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        _productRepositoryMock.Setup(x => x.UpdateItemAsync(existingProduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var actualResult = await _productService.UpdateItemAsync(existingProduct, CancellationToken.None);

        // Assert
        Assert.Equal(1, actualResult);
        _productRepositoryMock.Verify(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.UpdateItemAsync(existingProduct, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenProductIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        // Arrange
        Product? existingProduct = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _productService.UpdateItemAsync(existingProduct!, CancellationToken.None));
        _productRepositoryMock.Verify(x => x.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _productRepositoryMock.Verify(x => x.UpdateItemAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenProductDoesNotExist_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var nonExistingProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(nonExistingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _productService.UpdateItemAsync(nonExistingProduct, CancellationToken.None));
        _productRepositoryMock.Verify(x => x.GetItemAsync(nonExistingProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.UpdateItemAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenProductExists_ShouldDeleteProductAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        var existingProduct = _fixture.Create<Product>();
        _productRepositoryMock.Setup(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        _productRepositoryMock.Setup(x => x.DeleteItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var actualResult = await _productService.DeleteItemAsync(existingProduct.Id, CancellationToken.None);

        // Assert
        Assert.Equal(1, actualResult);
        _productRepositoryMock.Verify(x => x.GetItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.DeleteItemAsync(existingProduct.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenProductDoesNotExist_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _productRepositoryMock.Reset();
        const int nonExistingProductId = 1;
        _productRepositoryMock.Setup(x => x.GetItemAsync(nonExistingProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _productService.DeleteItemAsync(nonExistingProductId, CancellationToken.None));
        _productRepositoryMock.Verify(x => x.GetItemAsync(nonExistingProductId, It.IsAny<CancellationToken>()), Times.Once);
        _productRepositoryMock.Verify(x => x.DeleteItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}