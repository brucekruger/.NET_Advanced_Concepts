using AutoMapper;
using CatalogService.Application.DTOs;
using CatalogService.Domain.Entities;

namespace CatalogService.Api.Mapping;

/// <summary>
/// AutoMapper configuration for domain-to-DTO mappings.
/// </summary>
public class MappingProfile : Profile
{
    /// <inheritdoc />
    public MappingProfile()
    {
        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ReverseMap();

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ReverseMap();
    }
}
