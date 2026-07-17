using AutoFixture;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Services;
using Moq;

namespace CatalogService.UnitTests;

public class CategoryServiceTests
{
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly ICatalogService<Category> _categoryService;
    private readonly Fixture _fixture;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _categoryService = new CategoryService(_categoryRepositoryMock.Object);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetItemsAsync_WhenHasCategories_ShouldReturnAllCategoriesAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        var expectedCategories = _fixture.CreateMany<Category>(3).ToArray();
        _categoryRepositoryMock.Setup(x => x.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCategories);

        // Act
        var actualCategories = (await _categoryService.GetItemsAsync(CancellationToken.None)).ToArray();

        // Assert
        Assert.NotEmpty(actualCategories);
        Assert.Equal(expectedCategories.Length, actualCategories.Length);
        _categoryRepositoryMock.Verify(x => x.GetItemsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_WhenCategoryExists_ShouldReturnCategoryAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        var expectedCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(expectedCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var actualCategory = await _categoryService.GetItemAsync(expectedCategory.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(actualCategory);
        Assert.Equal(expectedCategory, actualCategory);
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(expectedCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_WhenCategoryDoesNotExist_ShouldReturnNullAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        const int expectedCategoryId = 1;

        // Act
        var actualCategory = await _categoryService.GetItemAsync(expectedCategoryId, CancellationToken.None);

        // Assert
        Assert.Null(actualCategory);
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(expectedCategoryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WhenCategoryIsValid_ShouldReturnAddedRowCountAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.AddItemAsync(expectedCategory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actualResult = await _categoryService.AddItemAsync(expectedCategory, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
        _categoryRepositoryMock.Verify(x => x.AddItemAsync(expectedCategory, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WhenCategoryIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        // Arrange
        Category? nullCategory = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _categoryService.AddItemAsync(nullCategory!, CancellationToken.None));
        _categoryRepositoryMock.Verify(x => x.AddItemAsync(nullCategory, CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_WhenCategoryAlreadyExists_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        var existingCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(existingCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _categoryService.AddItemAsync(existingCategory, CancellationToken.None));
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(existingCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepositoryMock.Verify(x => x.AddItemAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task UpdateItemAsync_WhenCategoryExists_ShouldReturnUpdatedRowCountAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(expectedCategory.Id, CancellationToken.None))
            .ReturnsAsync(expectedCategory);
        _categoryRepositoryMock.Setup(x => x.UpdateItemAsync(expectedCategory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actualResult = await _categoryService.UpdateItemAsync(expectedCategory, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(expectedCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepositoryMock.Verify(x => x.UpdateItemAsync(expectedCategory, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenCategoryIsNull_ShouldThrowArgumentNullExceptionAsync()
    {
        // Arrange
        Category? existingCategory = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _categoryService.UpdateItemAsync(existingCategory!, CancellationToken.None));
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _categoryRepositoryMock.Verify(x => x.UpdateItemAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenCategoryDoesNotExist_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        var nonExistingCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(nonExistingCategory.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _categoryService.UpdateItemAsync(nonExistingCategory, CancellationToken.None));
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(nonExistingCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepositoryMock.Verify(x => x.UpdateItemAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenCategoryExists_ShouldReturnDeletedRowCountAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(expectedCategory.Id, CancellationToken.None))
            .ReturnsAsync(expectedCategory);
        _categoryRepositoryMock.Setup(x => x.DeleteItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actualResult = await _categoryService.DeleteItemAsync(expectedCategory.Id, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(expectedCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepositoryMock.Verify(x => x.DeleteItemAsync(expectedCategory.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenCategoryDoesNotExist_ShouldThrowInvalidOperationExceptionAsync()
    {
        // Arrange
        _categoryRepositoryMock.Reset();
        const int nonExistingCategoryId = 1;
        _categoryRepositoryMock.Setup(x => x.GetItemAsync(nonExistingCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _categoryService.DeleteItemAsync(nonExistingCategoryId, CancellationToken.None));
        _categoryRepositoryMock.Verify(x => x.GetItemAsync(nonExistingCategoryId, It.IsAny<CancellationToken>()), Times.Once);
        _categoryRepositoryMock.Verify(x => x.DeleteItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}