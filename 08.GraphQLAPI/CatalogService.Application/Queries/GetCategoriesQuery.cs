using CatalogService.Application.DTOs;
using MediatR;

namespace CatalogService.Application.Queries;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;
