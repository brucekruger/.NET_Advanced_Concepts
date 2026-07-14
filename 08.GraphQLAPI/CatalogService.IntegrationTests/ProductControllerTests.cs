using AutoFixture;
using CatalogService.Api.Controllers;
using CatalogService.Api.Filters;
using CatalogService.Api.Interfaces;
using CatalogService.Api.Models;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CatalogService.IntegrationTests;

public class ProductControllerTests
{
    private readonly ProductController _productController;
    private readonly Mock<ICatalogService<Product>> _mockProductService = new();
    private readonly Mock<ICatalogService<Category>> _mockCategoryService = new();
    private readonly Mock<IHateoasLinkBuilder> _mockLinkBuilder = new();
    private readonly Fixture _fixture;

    public ProductControllerTests()
    {
        _productController = new ProductController(_mockProductService.Object, _mockCategoryService.Object, _mockLinkBuilder.Object);
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Constructor_NullProductService_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new ProductController(null!, _mockCategoryService.Object, _mockLinkBuilder.Object));
    }

    [Fact]
    public async Task GetAllProducts_ReturnsOkResultAsync()
    {
        // Arrange
        var products = _fixture.CreateMany<Product>(4).ToArray();
        _mockProductService.Setup(s => s.GetItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);
        _mockLinkBuilder.Setup(l => l.BuildProductLinks(It.IsAny<int>(), It.IsAny<int>()))
            .Returns([]);
        _mockLinkBuilder.Setup(l => l.BuildProductCollectionLinks())
            .Returns([]);
        var filter = new ProductFilter();

        // Act
        var actualResult = await _productController.Get(filter, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actualResult.Result);
        Assert.NotNull(okResult.Value);
        
        var responseType = okResult.Value.GetType();
        Assert.True(responseType.IsAnonymousType(), "Response should be an anonymous type with items and links");
        
        var itemsProperty = responseType.GetProperty("items");
        Assert.NotNull(itemsProperty);
        var items = itemsProperty.GetValue(okResult.Value);
        var returnValue = Assert.IsType<ProductHateoasDto[]>(items);
        Assert.Equal(products.Length, returnValue.Length);
        
        var linksProperty = responseType.GetProperty("links");
        Assert.NotNull(linksProperty);
        var links = linksProperty.GetValue(okResult.Value);
        Assert.NotNull(links);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenProductMissing()
    {
        // Arrange
        _mockProductService.Setup(s => s.GetItemAsync(321, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        // Act
        var actualResult = await _productController.Get(321, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NotFoundObjectResult>(actualResult.Result);
    }

    [Fact]
    public async Task Post_ReturnsNotFound_WhenProductMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(321, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);
        var productDto = new ProductDto
        {
            Id = 1,
            Name = "Test product",
            Description = "Test product description",
            Image = new Uri("https://www.example.com/test-product.png"),
            Price = 777m,
            Amount = 1000,
            CategoryId = 321
        };

        // Act
        var actualResult = await _productController.Post(productDto, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<BadRequestObjectResult>(actualResult);
    }

    [Fact]
    public async Task Put_ReturnsNotFound_WhenProductMissing()
    {
        // Arrange
        _mockProductService.Setup(s => s.GetItemAsync(321, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var productDto = new ProductDto
        {
            Id = 321,
            Name = "Test product",
            Description = "Test product description",
            Image = new Uri("https://www.example.com/test-product.png"),
            Price = 777m,
            Amount = 1000,
            CategoryId = 1
        };

        // Act
        var actualResult = await _productController.Put(321, productDto, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NotFoundResult>(actualResult);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenProductMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(321, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        // Act
        var actualResult = await _productController.Delete(321, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NoContentResult>(actualResult);
    }
}
