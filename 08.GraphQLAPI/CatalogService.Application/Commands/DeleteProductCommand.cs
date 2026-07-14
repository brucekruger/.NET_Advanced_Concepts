using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteProductCommand(int Id) : IRequest<bool>;
