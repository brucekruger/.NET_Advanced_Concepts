using System.Text;

namespace CatalogService.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Uri? Image { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Product>? Products { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"Id: {Id}");
        stringBuilder.AppendLine($"Name: {Name}");
        stringBuilder.AppendLine($"Image_Url: {Image?.AbsoluteUri}");

        stringBuilder.AppendLine($"ParentId: {ParentId}");
        var parentCategory = ParentId != null ? Parent?.Name : string.Empty;
        stringBuilder.AppendLine($"Parent Category: {parentCategory}");

        var productsCount = Products?.Count ?? 0;
        stringBuilder.AppendLine($"Products count: {productsCount}");

        return stringBuilder.ToString();
    }
}