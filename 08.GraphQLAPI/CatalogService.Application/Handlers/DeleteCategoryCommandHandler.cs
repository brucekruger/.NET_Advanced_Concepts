using CatalogService.Application.Commands;
using CatalogService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteCategoryCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return false;

        var hasChildren = await _dbContext.Categories.AnyAsync(c => c.ParentId == request.Id, cancellationToken);
        if (hasChildren) throw new InvalidOperationException("Cannot delete category with child categories.");

        // Delete related products
        var relatedProducts = _dbContext.Products.Where(p => p.CategoryId == request.Id);
        _dbContext.Products.RemoveRange(relatedProducts);

        // Delete category
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
