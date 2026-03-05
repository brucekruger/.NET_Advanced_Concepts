namespace CartService.Api.Models;

public record CartDto(string CartId, ICollection<CartItemDto> Items);
