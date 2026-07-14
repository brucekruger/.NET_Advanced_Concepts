using MediatR;
using CatalogService.Application.DTOs;
using CatalogService.Application.Queries;
using CatalogService.Api.GraphQL.Types;
using Microsoft.AspNetCore.Authorization;

namespace CatalogService.Api.GraphQL.Queries;

/// <summary>
/// Root query type for the GraphQL schema.
/// All query fields require authentication via JWT Bearer token.
/// </summary>
[Authorize]
public class Query
{
    /// <summary>
    /// Retrieves all product categories.
    /// </summary>
    [GraphQLType(typeof(NonNullType<ListType<NonNullType<CategoryType>>>))]
    [GraphQLDescription("Gets all product categories")]
    public Task<IEnumerable<CategoryDto>> GetCategories(
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new GetCategoriesQuery(), cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated products with optional category filtering.
    /// </summary>
    [GraphQLType(typeof(NonNullType<PaginatedProductsType>))]
    [GraphQLDescription("Gets paginated products with optional category filtering")]
    public Task<PaginatedProductsDto> GetProducts(
        [Service] IMediator mediator,
        [GraphQLDescription("Filter by category ID")] int? categoryId = null,
        [GraphQLDescription("Page number (1-based)")] int pageNumber = 1,
        [GraphQLDescription("Page size")] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return mediator.Send(
            new GetProductsQuery(categoryId, pageNumber, pageSize),
            cancellationToken);
    }
}
