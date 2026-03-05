using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CatalogService.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri? Image { get; set; }
    public decimal Price { get; set; }
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"ID: {Id}");
        stringBuilder.AppendLine($"Name: {Name}");
        stringBuilder.AppendLine($"Description: {Description}");
        stringBuilder.AppendLine($"Price: {Price:C2}");
        stringBuilder.AppendLine($"Amount: {Amount}");
        stringBuilder.AppendLine($"Category: {Category?.Name}");

        return stringBuilder.ToString();
    }
}