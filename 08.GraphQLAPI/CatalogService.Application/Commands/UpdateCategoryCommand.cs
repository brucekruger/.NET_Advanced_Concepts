using CatalogService.Application.DTOs;
using MediatR;

namespace CatalogService.Application.Commands;

public record UpdateCategoryCommand(
    int Id,
    string Name,
    Uri? Image,
    int? ParentId) : IRequest<CategoryDto>;
