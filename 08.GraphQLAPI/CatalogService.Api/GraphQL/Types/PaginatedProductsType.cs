using CatalogService.Application.Queries;

namespace CatalogService.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for paginated products response.
/// </summary>
public class PaginatedProductsType : ObjectType<PaginatedProductsDto>
{
    /// <summary>
    /// Configures the GraphQL type descriptor for <see cref="PaginatedProductsDto"/>.
    /// </summary>
    /// <param name="descriptor">The object type descriptor to configure.</param>
    protected override void Configure(IObjectTypeDescriptor<PaginatedProductsDto> descriptor)
    {
        descriptor
            .Description("Represents a paginated collection of products");

        descriptor
            .Field(p => p.Products)
            .Description("The products in this page")
            .Type<ListType<NonNullType<ProductType>>>();

        descriptor
            .Field(p => p.TotalCount)
            .Description("Total number of products matching the filter")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field(p => p.PageNumber)
            .Description("Current page number")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field(p => p.PageSize)
            .Description("Number of items per page")
            .Type<NonNullType<IntType>>();
    }
}
