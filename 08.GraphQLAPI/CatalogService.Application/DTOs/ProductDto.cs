namespace CatalogService.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri? Image { get; set; }
    public decimal Price { get; set; }
    public int Amount { get; set; }
    public int CategoryId { get; set; }

    // Example resolver methods (or resolver class)
    public int GetId(CategoryDto parent) => parent.Id;
    public Uri? GetImage(CategoryDto parent) => parent.Image;
    public string? GetName(CategoryDto parent) => parent.Name;
    public int? GetParentId(CategoryDto parent) => parent.Parent?.Id;

    // Or when configuring with descriptor:
    /*descriptor
      .Field("id")
      .Resolve(ctx =>
      {
          var dto = ctx.Parent<CatalogService.Application.DTOs.CategoryDto>();
          var entity = new CatalogService.Domain.Entities.Category { Id = dto.Id, Name = dto.Name, Image = dto.Image, *//*...*//* };
          return entity.Id;
      });*/
}
