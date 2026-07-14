using CatalogService.Application.DTOs;
using MediatR;

namespace CatalogService.Application.Commands;

public record UpdateProductCommand(
    int Id,
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId) : IRequest<ProductDto>;
