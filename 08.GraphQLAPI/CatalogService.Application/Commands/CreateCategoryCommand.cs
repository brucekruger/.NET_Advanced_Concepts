using CatalogService.Application.DTOs;
using MediatR;

namespace CatalogService.Application.Commands;

public record CreateCategoryCommand(
    string Name,
    Uri? Image = null,
    int? ParentId = null) : IRequest<CategoryDto>;
