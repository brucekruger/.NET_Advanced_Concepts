using CatalogService.Api.GraphQL.Resolvers;
using CatalogService.Application.DTOs;

namespace CatalogService.Api.GraphQL.Types;

/// <summary>
/// Represents the GraphQL object type for a product category.
/// </summary>
public class CategoryType : ObjectType<CategoryDto>
{
    /// <summary>
    /// Configures the GraphQL type descriptor for <see cref="CategoryDto"/>.
    /// Defines fields and their GraphQL types and descriptions.
    /// </summary>
    /// <param name="descriptor">The object type descriptor to configure.</param>
    protected override void Configure(IObjectTypeDescriptor<CategoryDto> descriptor)
    {
        descriptor
            .Description("Represents a product category");

        descriptor
            .Field(c => c.Id)
            .Description("The category ID")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field(c => c.Name)
            .Description("The category name")
            .Type<StringType>();

        descriptor
            .Field(c => c.Image)
            .Description("The category image URL")
            .Type<UriType>();

        descriptor
            .Field(c => c.Parent)
            .Description("The parent category")
            .Type<CategoryType>();

        descriptor
            .Field(c => c.Products)
            .Description("Products in this category")
            .Type<ListType<ProductType>>()
            .ResolveWith<CategoryResolvers>(r => r.GetProducts(default!, default!, default!));
    }
}
