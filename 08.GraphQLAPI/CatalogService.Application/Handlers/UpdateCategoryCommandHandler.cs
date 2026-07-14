using AutoMapper;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
                           .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                       ?? throw new KeyNotFoundException($"Category with ID {request.Id} not found");

        category.Name = request.Name;
        category.Image = request.Image;
        category.ParentId = request.ParentId;

        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }
}
