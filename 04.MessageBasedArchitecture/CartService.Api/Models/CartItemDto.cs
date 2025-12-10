using System.ComponentModel.DataAnnotations;

namespace CartService.Api.Models;

public record CartItemDto(int Id, string? Name, ImageDto? Image, decimal Price, int Quantity);