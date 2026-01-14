using CatalogService.Api.Extensions;
using CatalogService.Api.Filters;
using CatalogService.Api.Models;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using CatalogService.Api.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CatalogService.Api.Controllers;
/// <summary>
/// Controller for managing product operations V1.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/products")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class ProductController : ControllerBase
{
    private readonly ICatalogService<Category> _categoryService;
    private readonly ICatalogService<Product> _productService;
    private readonly IHateoasLinkBuilder _linkBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductController"/> class.
    /// </summary>
    /// <param name="productService"></param>
    /// <param name="categoryService"></param>
    /// <param name="linkBuilder">The HATEOAS link builder for generating hypermedia links.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ProductController(ICatalogService<Product> productService, ICatalogService<Category> categoryService, IHateoasLinkBuilder linkBuilder)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _linkBuilder = linkBuilder ?? throw new ArgumentNullException(nameof(linkBuilder));
    }

    // GET: api/products
    /// <summary>
    /// Retrieves all product information with HATEOAS links (Level 3 REST).
    /// </summary>
    /// <param name="filter"><see cref="ProductFilter"/></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The product list with hypermedia links</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Get([FromQuery] ProductFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var allProducts = await _productService.GetProductsByCategoryPagedAsync(filter, cancellationToken);

            var productDtos = allProducts
                .Select(p => new ProductHateoasDto(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Image,
                    p.Price,
                    p.Amount,
                    p.CategoryId,
                    _linkBuilder.BuildProductLinks(p.Id, p.CategoryId)
                ))
                .ToArray();

            var response = new
            {
                items = productDtos,
                links = _linkBuilder.BuildProductCollectionLinks()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET api/products/5
    /// <summary>
    /// Retrieves product information for the specified product ID with HATEOAS links (Level 3 REST).
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The product details with hypermedia links if found; otherwise, a not found or bad request response.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductHateoasDto>> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.GetItemAsync(id, cancellationToken);

            if (product == null)
            {
                return NotFound(id);
            }

            var productDto = new ProductHateoasDto(
                product.Id,
                product.Name,
                product.Description,
                product.Image,
                product.Price,
                product.Amount,
                product.CategoryId,
                _linkBuilder.BuildProductLinks(product.Id, product.CategoryId)
            );

            return Ok(productDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST api/products
    /// <summary>
    /// Creates new product.
    /// </summary>
    /// <param name="productDto">The product DTO.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The created product item if successful; otherwise, a bad request or error response.</returns>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Post([FromBody] ProductDto productDto, CancellationToken cancellationToken)
    {
        try
        {
            var parentCategory = await _categoryService.GetItemAsync(productDto.CategoryId, cancellationToken);

            if (parentCategory == null)
            {
                return BadRequest($"Parent category with ID {productDto.CategoryId} does not exist.");
            }

            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Image = productDto.Image,
                Price = productDto.Price,
                Amount = productDto.Amount,
                CategoryId = productDto.CategoryId
            };

            await _productService.AddItemAsync(product, cancellationToken);

            var createdProductDto = new ProductDto(product.Id, product.Name, product.Description, product.Image, product.Price, product.Amount, product.CategoryId);
            return CreatedAtAction(nameof(Get), new { id = product.Id }, createdProductDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT api/products/5
    /// <summary>
    /// Updates existing product.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="productDto">The updated product DTO.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The updated product item if successful; otherwise, a bad request or error response.</returns>
    [HttpPut("{id:int}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Put(int id, [FromBody] ProductDto productDto, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.GetItemAsync(id, cancellationToken);

            if (product == null)
            {
                return NotFound();
            }

            var parentCategory = await _categoryService.GetItemAsync(productDto.CategoryId, cancellationToken);

            if (parentCategory == null)
            {
                return BadRequest($"Parent category with ID {productDto.CategoryId} does not exist.");
            }

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Image = productDto.Image;
            product.Price = productDto.Price;
            product.Amount = productDto.Amount;
            product.CategoryId = productDto.CategoryId;

            await _productService.UpdateItemAsync(product, cancellationToken);

            var updatedProductDto = new ProductDto(product.Id, product.Name, product.Description, product.Image, product.Price, product.Amount, product.CategoryId);
            return Ok(updatedProductDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE api/products/5
    /// <summary>
    /// Removes product.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content if successful or not found; otherwise, a bad request response.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.GetItemAsync(id, cancellationToken);

            if (product == null)
            {
                return NoContent();
            }

            await _productService.DeleteItemAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}