using MediatR;

namespace CatalogService.Application.Commands;
public record DeleteCategoryCommand(int Id) : IRequest<bool>;
