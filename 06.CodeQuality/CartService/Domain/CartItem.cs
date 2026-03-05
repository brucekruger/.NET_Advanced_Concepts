using System.ComponentModel.DataAnnotations;
using LiteDB;

namespace CartService.Domain;

public class CartItem
{
    [BsonId]
    public int Id { get; set; }

    [Required]
    public string? Name { get; set; }

    public Image? Image { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public string CartId { get; set; } = null!;

    public override string ToString()
    {
        return $"CartItem {Id}: {Name} (Quantity: {Quantity}, Price: {Price:C})";
    }
}