using CartService.Api.Models;
using CartService.Application.Interfaces;
using CartService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace CartService.Api.Controllers.V1;

/// <summary>
/// Controller for managing cart operations V1.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
[Authorize]  // All endpoints require both roles
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CartController"/> class.
    /// </summary>
    /// <param name="cartService">The cart service to use for cart operations.</param>
    public CartController(ICartService cartService)
    {
        _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
    }

    // GET api/v1/cart/34f57c9a-b66f-46a6-87a4-8d6aa1a072fd
    /// <summary>
    /// Retrieves cart information for the specified cart ID.
    /// </summary>
    /// <param name="id">The unique identifier of the cart.</param>
    /// <returns>The cart details if found; otherwise, a not found or bad request response.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CartDto> GetCartInfo([FromRoute] string id)
    {
        try
        {
            var cart = _cartService.GetCart(id);
            if (cart == null)
            {
                return NotFound(id);
            }

            var cartItemDtos = cart.CartItems.Select(item => new CartItemDto(
                item.Id,
                item.Name ?? string.Empty,
                new ImageDto(item.Image?.Url, item.Image?.AltText),
                item.Price,
                item.Quantity)).ToArray();

            var cartDto = new CartDto(cart.Id, cartItemDtos);

            return Ok(cartDto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    // POST api/v1/cart
    /// <summary>
    /// Adds an item to the specified cart.
    /// </summary>
    /// <param name="cartDto">The cart DTO containing the item to add.</param>
    /// <returns>The created cart item if successful; otherwise, a bad request or error response.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CartItem> AddItemToCart([FromBody] CartDto cartDto)
    {
        try
        {
            var cartItemDto = cartDto.Items.FirstOrDefault();
            if (cartItemDto == null)
            {
                return BadRequest("Cart must contain at least one item.");
            }

            var cartItemImage = new Image
            {
                Url = cartItemDto.Image?.Url,
                AltText = cartItemDto.Image?.AltText
            };

            var cartItem = new CartItem
            {
                CartId = cartDto.CartId,
                Id = cartItemDto.Id,
                Name = cartItemDto.Name,
                Image = cartItemImage,
                Price = cartItemDto.Price,
                Quantity = cartItemDto.Quantity
            };

            var createdCartItem = _cartService.AddItem(cartDto.CartId, cartItem);

            return CreatedAtAction(nameof(GetCartInfo), new { id = createdCartItem.Id, version = "1.0" }, createdCartItem);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    // DELETE api/v1/cart/34f57c9a-b66f-46a6-87a4-8d6aa1a072fd/cart-items/5
    /// <summary>
    /// Removes an item from the specified cart.
    /// </summary>
    /// <param name="cartId">The unique identifier of the cart.</param>
    /// <param name="cartItemId">The unique identifier of the cart item to remove.</param>
    /// <returns>No content if successful or not found; otherwise, a bad request response.</returns>
    [HttpDelete("{cartId}/cart-items/{cartItemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult DeleteItem([FromRoute] string cartId, [FromRoute] int cartItemId)
    {
        try
        {
            var cart = _cartService.GetCart(cartId);
            if (cart == null)
            {
                return NoContent();
            }

            _cartService.RemoveItem(cartId, cartItemId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }
}