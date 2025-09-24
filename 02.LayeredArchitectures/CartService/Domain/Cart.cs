using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CartService.Domain;

public class Cart
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required]
    public string? Name { get; set; }
    public Image? Image { get; set; }
    [Required]
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Id: {Id}");
        sb.AppendLine($"Name: {Name}");
        sb.AppendLine($"Image_Url: {Image?.Url}");
        sb.AppendLine($"Image_AltText: {Image?.AltText}");
        sb.AppendLine($"Price: {Price}");
        sb.AppendLine($"Quantity: {Quantity}");

        return sb.ToString();
    }
}
