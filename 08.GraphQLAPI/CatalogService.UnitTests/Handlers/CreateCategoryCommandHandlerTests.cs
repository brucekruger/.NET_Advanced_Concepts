using AutoMapper;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CatalogService.UnitTests.Handlers;

/// <summary>
/// Unit tests for CreateCategoryCommandHandler.
/// Tests command handling, DTO mapping, and database operations.
/// </summary>
public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateCategoryCommandHandler(_mockDbContext.Object, _mockMapper.Object);

        // Provide a mock DbSet for Categories so handler can call Add without NullReferenceException
        var mockCategoryDbSet = new Mock<DbSet<Category>>();
        _mockDbContext.SetupGet(db => db.Categories).Returns(mockCategoryDbSet.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesCategory()
    {
        // Arrange
        var command = new CreateCategoryCommand("Electronics");
        var expectedDto = new CategoryDto { Id = 1, Name = "Electronics" };

        _mockMapper
            .Setup(m => m.Map<CategoryDto>(It.IsAny<Category>()))
            .Returns(expectedDto);

        _mockDbContext
            .Setup(db => db.Categories.Add(It.IsAny<Category>()))
            .Verifiable();

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Electronics", result.Name);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_MapsCorrectly()
    {
        // Arrange
        var command = new CreateCategoryCommand("Books", new Uri("https://example.com/books.jpg"));
        var createdCategory = new Category { Id = 2, Name = "Books" };
        var expectedDto = new CategoryDto { Id = 2, Name = "Books" };

        _mockMapper
            .Setup(m => m.Map<CategoryDto>(It.IsAny<Category>()))
            .Returns(expectedDto);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<CategoryDto>(It.IsAny<Category>()), Times.Once);
        Assert.Equal(2, result.Id);
        Assert.Equal("Books", result.Name);
    }
}
