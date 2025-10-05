using AutoFixture;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CatalogService.IntegrationTests;

public class CategoryRepositoryTests : IAsyncDisposable
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly Fixture _fixture;

    public CategoryRepositoryTests()
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(CategoryRepositoryTests))
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _applicationDbContext = new ApplicationDbContext(dbContextOptions);
        _categoryRepository = new CategoryRepository(_applicationDbContext);

        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GetItemsAsync_WhenHasCategories_ShouldReturnAllCategoriesAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        var expectedCategories = _fixture.CreateMany<Category>(3).ToArray();
        await _applicationDbContext.Categories.AddRangeAsync(expectedCategories);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualCategories = (await _categoryRepository.GetItemsAsync(CancellationToken.None)).ToArray();

        // Assert
        Assert.NotEmpty(actualCategories);
        Assert.Equal(expectedCategories.Length, actualCategories.Length);
    }

    [Fact]
    public async Task GetItemAsync_WhenCategoryExists_ShouldReturnCategoryAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        var expectedCategory = _fixture.Create<Category>();
        await _applicationDbContext.Categories.AddAsync(expectedCategory);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualCategory = await _categoryRepository.GetItemAsync(expectedCategory.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(actualCategory);
        Assert.Equal(expectedCategory, actualCategory);
    }

    [Fact]
    public async Task GetItemAsync_WhenCategoryDoesNotExist_ShouldReturnNullAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedNonExistingCategoryId = 100500;

        // Act
        var actualCategory = await _categoryRepository.GetItemAsync(expectedNonExistingCategoryId, CancellationToken.None);

        // Assert
        Assert.Null(actualCategory);
    }

    [Fact]
    public async Task AddItemAsync_WhenCategoryIsValid_ShouldReturnAddedRowCountAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        expectedCategory.ParentId = null;
        expectedCategory.Parent = null;
        expectedCategory.Products = null;

        // Act
        var actualResult = await _categoryRepository.AddItemAsync(expectedCategory, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenCategoryExists_ShouldReturnUpdatedRowCountAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        expectedCategory.ParentId = null;
        expectedCategory.Parent = null;
        expectedCategory.Products = null;
        await _applicationDbContext.Categories.AddAsync(expectedCategory);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        expectedCategory.Name = "Updated category name";
        expectedCategory.Image = new Uri("https://updated-image-url.com/updated-category.jpg");
        var actualResult = await _categoryRepository.UpdateItemAsync(expectedCategory, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public async Task DeleteItemAsync_WhenCategoryExists_ShouldReturnDeletedRowCountAsync()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int expectedResult = 1;
        var expectedCategory = _fixture.Create<Category>();
        expectedCategory.ParentId = null;
        expectedCategory.Parent = null;
        expectedCategory.Products = null;
        await _applicationDbContext.Categories.AddAsync(expectedCategory);
        await _applicationDbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var actualResult = await _categoryRepository.DeleteItemAsync(expectedCategory.Id, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    public async ValueTask DisposeAsync()
    {
        await _applicationDbContext.DisposeAsync();
    }

    private async Task ResetDatabaseAsync()
    {
        await _applicationDbContext.Database.EnsureDeletedAsync();
        await _applicationDbContext.Database.EnsureCreatedAsync();
    }
}