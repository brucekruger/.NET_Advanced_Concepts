using AutoMapper;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedProductsDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedProductsDto> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);

        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

        return new PaginatedProductsDto(
            productDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
