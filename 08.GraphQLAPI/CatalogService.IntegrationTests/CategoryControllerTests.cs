using AutoFixture;
using CatalogService.Api.Controllers;
using CatalogService.Api.Interfaces;
using CatalogService.Api.Models;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CatalogService.IntegrationTests;

public class CategoryControllerTests
{
    private readonly CategoryController _categoryController;
    private readonly Mock<ICatalogService<Category>> _mockCategoryService = new();
    private readonly Mock<IHateoasLinkBuilder> _mockLinkBuilder = new();
    private readonly Fixture _fixture;

    public CategoryControllerTests()
    {
        _categoryController = new CategoryController(_mockCategoryService.Object, _mockLinkBuilder.Object);
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new CategoryController(null!, _mockLinkBuilder.Object));
    }

    [Fact]
    public async Task GetAllCategories_ReturnsOkResultAsync()
    {
        // Arrange
        var categories = _fixture.CreateMany<Category>(5).ToArray();
        _mockCategoryService.Setup(s => s.GetItemsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _mockLinkBuilder.Setup(l => l.BuildCategoryLinks(It.IsAny<int>()))
            .Returns([]);
        _mockLinkBuilder.Setup(l => l.BuildCategoryCollectionLinks())
            .Returns([]);

        // Act
        var actualResult = await _categoryController.Get(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actualResult.Result);
        Assert.NotNull(okResult.Value);
        
        var responseType = okResult.Value.GetType();
        Assert.True(responseType.IsAnonymousType(), "Response should be an anonymous type with items and links");
        
        var itemsProperty = responseType.GetProperty("items");
        Assert.NotNull(itemsProperty);
        var items = itemsProperty.GetValue(okResult.Value);
        var returnValue = Assert.IsType<CategoryHateoasDto[]>(items);
        Assert.Equal(categories.Length, returnValue.Length);
        
        var linksProperty = responseType.GetProperty("links");
        Assert.NotNull(linksProperty);
        var links = linksProperty.GetValue(okResult.Value);
        Assert.NotNull(links);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenCategoryMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(123, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        // Act
        var actualResult = await _categoryController.Get(123, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NotFoundObjectResult>(actualResult.Result);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenParentCategoryMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var categoryDto = new CategoryDto
        {
            Id = 123,
            Name = "TestCategory",
            Image = new Uri("https://www.example.com/test-category.png"),
            Parent = new CategoryDto { Id = 2 }
        };

        // Act
        var actualResult = await _categoryController.Post(categoryDto, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<BadRequestObjectResult>(actualResult);
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenCategoryMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);
        var categoryDto = new CategoryDto
        {
            Id = 123,
            Name = "TestCategory",
            Image = new Uri("https://www.example.com/test-category.png"),
            Parent = new CategoryDto { Id = 2 }
        };

        // Act
        var actualResult = await _categoryController.Put(123, categoryDto, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NotFoundResult>(actualResult);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenCategoryMissing()
    {
        // Arrange
        _mockCategoryService.Setup(s => s.GetItemAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act
        var actualResult = await _categoryController.Delete(123, true, CancellationToken.None);

        // Assert
        Assert.NotNull(actualResult);
        Assert.IsType<NoContentResult>(actualResult);
    }
}
