using CatalogService.Application.DTOs;
using CatalogService.Api.GraphQL.DataLoaders;

namespace CatalogService.Api.GraphQL.Types;

/// <summary>
/// Represents the GraphQL object type for a product.
/// </summary>
public class ProductType : ObjectType<ProductDto>
{
    /// <summary>
    /// Configures the GraphQL type descriptor for <see cref="ProductDto"/>.
    /// </summary>
    /// <param name="descriptor">The object type descriptor to configure.</param>
    protected override void Configure(IObjectTypeDescriptor<ProductDto> descriptor)
    {
        descriptor
            .Description("Represents a product");

        descriptor
            .Field(p => p.Id)
            .Description("The product ID")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field(p => p.Name)
            .Description("The product name")
            .Type<StringType>();

        descriptor
            .Field(p => p.Description)
            .Description("The product description")
            .Type<StringType>();

        descriptor
            .Field(p => p.Image)
            .Description("The product image URL")
            .Type<UriType>();

        descriptor
            .Field(p => p.Price)
            .Description("The product price")
            .Type<NonNullType<DecimalType>>();

        descriptor
            .Field(p => p.Amount)
            .Description("The product stock amount")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field(p => p.CategoryId)
            .Description("The category ID")
            .Type<NonNullType<IntType>>();

        descriptor
            .Field("category")
            .Description("The product category")
            .Type<CategoryType>()
            .Resolve(ctx =>
            {
                var parent = ctx.Parent<ProductDto>();
                if (parent == null)
                {
                    return null;
                }

                var loader = ctx.DataLoader<CategoryBatchDataLoader>();
                return loader.LoadAsync(parent.CategoryId, ctx.RequestAborted);
            });
    }
}
