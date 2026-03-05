namespace CatalogService.Api.Models;

public record ProductDto(int Id, string? Name, string? Description, Uri? Image, decimal Price, int Amount, int CategoryId);