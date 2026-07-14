using MediatR;
using CatalogService.Api.GraphQL.InputTypes;
using CatalogService.Api.GraphQL.Types;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Api.GraphQL.Mutations;

/// <summary>
/// Root mutation type for the GraphQL schema.
/// All mutation fields require Admin role authorization.
/// </summary>
[Authorize(Roles = "Admin")]
public class Mutation
{
    /// <summary>
    /// Creates a new product category.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<CategoryType>))]
    [GraphQLDescription("Creates a new product category")]
    public Task<CategoryDto> CreateCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category creation input")] CreateCategoryInput input,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(input.Name, input.Image, input.ParentId);
        return mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product category.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<CategoryType>))]
    [GraphQLDescription("Updates an existing product category")]
    public Task<CategoryDto> UpdateCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category ID")] int id,
        [GraphQLDescription("Category update input")] UpdateCategoryInput input,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(input.Id, input.Name, input.Image, input.ParentId);
        return mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a product category and all its related products.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Deletes a product category and all its related products")]
    public Task<bool> DeleteCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category ID")] int id,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
    }

    /// <summary>
    /// Creates a new product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<ProductType>))]
    [GraphQLDescription("Creates a new product")]
    public Task<ProductDto> CreateProduct(
        [Service] IMediator mediator,
        [GraphQLDescription("Product creation input")] CreateProductInput input,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            input.Name,
            input.Description,
            input.Image,
            input.Price,
            input.Amount,
            input.CategoryId);
        return mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<ProductType>))]
    [GraphQLDescription("Updates an existing product")]
    public Task<ProductDto> UpdateProduct(
        [Service] IMediator mediator,
        [GraphQLDescription("Product ID")] int id,
        [GraphQLDescription("Product update input")] UpdateProductInput input,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            input.Id,
            input.Name,
            input.Description,
            input.Image,
            input.Price,
            input.Amount,
            input.CategoryId);
        return mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Deletes a product")]
    public Task<bool> DeleteProduct(
        [Service] IMediator mediator,
        [GraphQLDescription("Product ID")] int id,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeleteProductCommand(id), cancellationToken);
    }
}
