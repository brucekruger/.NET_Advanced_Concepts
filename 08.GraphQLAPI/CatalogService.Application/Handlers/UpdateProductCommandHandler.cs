using AutoMapper;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
                          .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Product with ID {request.Id} not found");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Image = request.Image;
        product.Price = request.Price;
        product.Amount = request.Amount;
        product.CategoryId = request.CategoryId;

        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}
