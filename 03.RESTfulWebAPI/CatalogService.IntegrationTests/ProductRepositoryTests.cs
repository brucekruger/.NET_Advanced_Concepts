using AutoFixture;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CatalogService.IntegrationTests;

public class ProductRepositoryTests : IAsyncDisposable
{
    private readonly IRepository<Product> _productRepository;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly Fixture _fixture;

    public ProductRepositoryTests()
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(ProductRepositoryTests))
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _applicationDbContext = new ApplicationDbContext(dbContextOptions);
        _productRepository = new ProductRepository(_applicationDbContext);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetItemsAsync_WhenHasProducts_ShouldReturnAllProductsAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        var expectedProducts = _fixture.CreateMany<Product>(3).ToArray();
        await _applicationDbContext.Products.AddRangeAsync(expectedProducts);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualProducts = (await _productRepository.GetItemsAsync(CancellationToken.None)).ToArray();

        // Assert
        Assert.NotEmpty(actualProducts);
        Assert.Equal(expectedProducts.Length, actualProducts.Length);
    }

    [Fact]
    public async Task GetItemAsync_WhenProductExists_ShouldReturnProductAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        var expectedProduct = _fixture.Create<Product>();
        await _applicationDbContext.Products.AddAsync(expectedProduct);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualProduct = await _productRepository.GetItemAsync(expectedProduct.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(actualProduct);
        Assert.Equal(expectedProduct, actualProduct);
    }

    [Fact]
    public async Task GetItemAsync_WhenProductDoesNotExist_ShouldReturnNullAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int nonExistentProductId = 100500;

        // Act
        var actualProduct = await _productRepository.GetItemAsync(nonExistentProductId, CancellationToken.None);

        // Assert
        Assert.Null(actualProduct);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductIsValid_ShouldReturnAddedRowCountAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedResult = 2; //CategoryId is FK, so we need to add 2 rows: Category + Product
        var expectedProduct = _fixture.Create<Product>();

        // Act
        var actualResult = await _productRepository.AddItemAsync(expectedProduct, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenProductExists_ShouldReturnUpdatedRowCountAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedResult = 1;
        var expectedProduct = _fixture.Create<Product>();
        await _applicationDbContext.Products.AddAsync(expectedProduct);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        expectedProduct.Name = "Updated product name";
        expectedProduct.Image = new Uri("https://updated-image-url.com/updated-product.jpg");
        var actualResult = await _productRepository.UpdateItemAsync(expectedProduct, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenProductExists_ShouldDeleteProductAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        var productToDelete = _fixture.Create<Product>();
        const int expectedResult = 1;
        await _applicationDbContext.Products.AddAsync(productToDelete);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualResult = await _productRepository.DeleteItemAsync(productToDelete.Id, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }


    private async Task ResetDatabaseAsync()
    {
        await _applicationDbContext.Database.EnsureDeletedAsync();
        await _applicationDbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _applicationDbContext.DisposeAsync();
    }
}