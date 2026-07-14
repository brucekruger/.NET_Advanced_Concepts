using CatalogService.Application.DTOs;
using MediatR;

namespace CatalogService.Application.Queries;

public record GetProductsQuery(
    int? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedProductsDto>;

public record PaginatedProductsDto(
    IEnumerable<ProductDto> Products,
    int TotalCount,
    int PageNumber,
    int PageSize);
