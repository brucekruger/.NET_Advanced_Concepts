namespace CatalogService.Api.Models;

public record CategoryDto(int Id, string? Name, Uri? Image, int? ParentId);