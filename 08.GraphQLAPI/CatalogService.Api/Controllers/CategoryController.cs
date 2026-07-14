using CatalogService.Api.Interfaces;
using CatalogService.Api.Models;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using CatalogService.Application.DTOs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CatalogService.Api.Controllers;
/// <summary>
/// Controller for managing category operations V1.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/categories")]
[Authorize]  // Require authentication for all endpoints
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class CategoryController : ControllerBase
{
    private readonly ICatalogService<Category> _categoryService;
    private readonly IHateoasLinkBuilder _linkBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryController"/> class.
    /// </summary>
    /// <param name="categoryService">The category service to use for category operations.</param>
    /// <param name="linkBuilder">The HATEOAS link builder for generating hypermedia links.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CategoryController(ICatalogService<Category> categoryService, IHateoasLinkBuilder linkBuilder)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _linkBuilder = linkBuilder ?? throw new ArgumentNullException(nameof(linkBuilder));
    }

    // GET: api/categories
    /// <summary>
    /// Retrieves all category information with HATEOAS links (Level 3 REST).
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The category list with hypermedia links</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Get(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetItemsAsync(cancellationToken);
        var categoryDtos = categories.Select(c => new CategoryHateoasDto(
            c.Id,
            c.Name,
            c.Image,
            c.ParentId,
            _linkBuilder.BuildCategoryLinks(c.Id)
        )).ToArray();

        var response = new
        {
            items = categoryDtos,
            links = _linkBuilder.BuildCategoryCollectionLinks()
        };

        return Ok(response);
    }

    // GET api/categories/5
    /// <summary>
    /// Retrieves category information for the specified category ID with HATEOAS links (Level 3 REST).
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The category details with hypermedia links if found; otherwise, a not found or bad request response.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryHateoasDto>> Get([FromRoute] int id, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.GetItemAsync(id, cancellationToken);

            if (category == null)
            {
                return NotFound(id);
            }

            var categoryDto = new CategoryHateoasDto(
                category.Id,
                category.Name,
                category.Image,
                category.ParentId,
                _linkBuilder.BuildCategoryLinks(category.Id)
            );

            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST api/categories
    /// <summary>
    /// Creates new category.
    /// </summary>
    /// <param name="categoryDto">The category DTO.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The created category item if successful; otherwise, a bad request or error response.</returns>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Post([FromBody] CategoryDto categoryDto, CancellationToken cancellationToken)
    {
        try
        {
            Category? parentCategory = null;

            int? parentId = null;
            if (categoryDto.Parent != null)
            {
                parentId = categoryDto.Parent.Id;
                parentCategory = await _categoryService.GetItemAsync(parentId.Value, cancellationToken);

                if (parentCategory == null)
                {
                    return BadRequest($"Parent category with ID {parentId.Value} does not exist.");
                }
            }

            var category = new Category
            {
                Name = categoryDto.Name,
                Image = categoryDto.Image,
                ParentId = parentId
            };

            await _categoryService.AddItemAsync(category, cancellationToken);

            var createdCategoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Image = category.Image,
                Parent = category.ParentId.HasValue ? new CategoryDto { Id = category.ParentId.Value } : null
            };
            return CreatedAtAction(nameof(Get), new { id = category.Id }, createdCategoryDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT api/categories/5
    /// <summary>
    /// Updates existing category.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="categoryDto">The updated category DTO.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The updated category item if successful; otherwise, a bad request or error response.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Put(int id, [FromBody] CategoryDto categoryDto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.GetItemAsync(id, cancellationToken);

            if (category == null)
            {
                return NotFound();
            }

            category.Name = categoryDto.Name;
            category.Image = categoryDto.Image;
            category.ParentId = categoryDto.Parent?.Id;

            await _categoryService.UpdateItemAsync(category, cancellationToken);

            var updatedCategoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Image = category.Image,
                Parent = category.ParentId.HasValue ? new CategoryDto { Id = category.ParentId.Value } : null
            };
            return Ok(updatedCategoryDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE api/categories/5
    /// <summary>
    /// Removes category. Optionally deletes all associated products if cascadeDelete is true.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cascadeDelete">If true, deletes all products associated with this category before deleting the category. Default is false.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content if successful or not found; otherwise, a bad request response.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Delete([FromRoute] int id, [FromQuery] bool cascadeDelete = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryService.GetItemAsync(id, cancellationToken);

            if (category == null)
            {
                return NoContent();
            }

            // Cast to CategoryService to access the overloaded DeleteItemAsync with cascadeDelete parameter
            if (_categoryService is Infrastructure.Services.CategoryService categoryService)
            {
                await categoryService.DeleteItemAsync(id, cascadeDelete, cancellationToken);
            }
            else
            {
                await _categoryService.DeleteItemAsync(id, cancellationToken);
            }

            return NoContent();
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("REFERENCE constraint") == true)
        {
            return BadRequest(new { error = "Cannot delete category because it has associated products. Please delete or reassign the products first." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
