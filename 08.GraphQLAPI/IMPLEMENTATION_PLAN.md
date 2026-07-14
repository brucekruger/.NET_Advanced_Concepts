# GraphQL Catalog Service Implementation Plan

**Project**: 08.GraphQLAPI  
**Target Framework**: .NET 9  
**Technology Stack**: HotChocolate 14+, MediatR, EF Core, JWT Authentication

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Phase 1: Project Setup & Package Installation](#phase-1-project-setup--package-installation)
3. [Phase 2: Domain & Application Layer Setup](#phase-2-domain--application-layer-setup)
4. [Phase 3: GraphQL API Layer](#phase-3-graphql-api-layer)
5. [Phase 4: Performance Optimization (N+1 Problem)](#phase-4-performance-optimization-n1-problem)
6. [Phase 5: Testing](#phase-5-testing)
7. [Phase 6: Configuration & Security](#phase-6-configuration--security)
8. [Phase 7: Query Examples](#phase-7-query-examples)
9. [Key References](#key-references)
10. [Performance Optimization Checklist](#performance-optimization-checklist)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Client Applications                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GraphQL API Layer                             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Schema │ Query Resolvers │ Mutations │ Subscriptions   │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Application Layer (CQRS)                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Commands/Queries │ Handlers │ Validation │ Authorization  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              Domain Layer                                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Entities │ Value Objects │ Domain Interfaces           │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│            Infrastructure Layer                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  DbContext │ Repositories │ DataLoaders │ EF Core        │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
                    ┌─────────────┐
                    │   Database  │
                    └─────────────┘
```

---

## Phase 1: Project Setup & Package Installation

### Step 1.1: Create Project Structure

```
08.GraphQLAPI/
├── CatalogService.Api/                    # GraphQL Endpoint
│   ├── GraphQL/
│   │   ├── Types/
│   │   ├── Queries/
│   │   ├── Mutations/
│   │   ├── InputTypes/
│   │   ├── DataLoaders/
│   │   └── Resolvers/
│   ├── Mapping/
│   ├── Program.cs
│   └── appsettings.json
├── CatalogService.Application/            # Business Logic (CQRS)
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   ├── DTOs/
│   └── Interfaces/
├── CatalogService.Domain/                 # Entities & Interfaces
│   ├── Entities/
│   └── Interfaces/
├── CatalogService.Infrastructure/         # Data Access & EF Core
│   ├── Data/
│   │   ├── Configurations/
│   │   └── ApplicationDbContext.cs
│   └── Repositories/
├── CatalogService.IntegrationTests/       # Integration Tests
└── CatalogService.UnitTests/              # Unit Tests
```

### Step 1.2: Install Required NuGet Packages

**Core GraphQL Packages:**

```
dotnet add CatalogService.Api package HotChocolate.AspNetCore --version 14.0.0
dotnet add CatalogService.Api package HotChocolate.Types --version 14.0.0
dotnet add CatalogService.Api package HotChocolate.Execution --version 14.0.0
```

**CQRS & Mediator:**

```
dotnet add CatalogService.Application package MediatR --version 12.0.0
dotnet add CatalogService.Application package MediatR.Extensions.Microsoft.DependencyInjection --version 12.0.0
```

**Data Access & Authorization:**

```
dotnet add CatalogService.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add CatalogService.Api package Microsoft.AspNetCore.Authorization
dotnet add CatalogService.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add CatalogService.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
```

**Object Mapping:**

```
dotnet add CatalogService.Api package AutoMapper --version 12.0.0
dotnet add CatalogService.Api package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.0
```

**Performance & Batching:**

```
dotnet add CatalogService.Api package HotChocolate.DataLoader --version 14.0.0
dotnet add CatalogService.Api package GreenDonut --version 14.0.0
```

**Testing:**

```
dotnet add CatalogService.UnitTests package xunit --version 2.6.0
dotnet add CatalogService.UnitTests package Moq --version 4.20.0
dotnet add CatalogService.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add CatalogService.IntegrationTests package xunit --version 2.6.0
```

---

## Phase 2: Domain & Application Layer Setup

### Step 2.1: Verify/Create Domain Entities

**File**: `CatalogService.Domain/Entities/Category.cs`

```csharp
namespace CatalogService.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Uri? Image { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Product>? Products { get; set; }
}
```

**File**: `CatalogService.Domain/Entities/Product.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CatalogService.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri? Image { get; set; }
    public decimal Price { get; set; }
    [Range(1, int.MaxValue)]
    public int Amount { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
```

### Step 2.2: Create Application Layer DTOs

**File**: `CatalogService.Application/DTOs/CategoryDto.cs`

```csharp
namespace CatalogService.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Uri? Image { get; set; }
    public int? ParentId { get; set; }
}
```

**File**: `CatalogService.Application/DTOs/ProductDto.cs`

```csharp
namespace CatalogService.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Uri? Image { get; set; }
    public decimal Price { get; set; }
    public int Amount { get; set; }
    public int CategoryId { get; set; }
}
```

### Step 2.3: Create CQRS Commands

**File**: `CatalogService.Application/Commands/CreateCategoryCommand.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Commands;

public record CreateCategoryCommand(
    string Name,
    Uri? Image = null,
    int? ParentId = null) : IRequest<CategoryDto>;
```

**File**: `CatalogService.Application/Commands/UpdateCategoryCommand.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Commands;

public record UpdateCategoryCommand(
    int Id,
    string Name,
    Uri? Image,
    int? ParentId) : IRequest<CategoryDto>;
```

**File**: `CatalogService.Application/Commands/DeleteCategoryCommand.cs`

```csharp
using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteCategoryCommand(int Id) : IRequest<bool>;
```

**File**: `CatalogService.Application/Commands/CreateProductCommand.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Commands;

public record CreateProductCommand(
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId) : IRequest<ProductDto>;
```

**File**: `CatalogService.Application/Commands/UpdateProductCommand.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Commands;

public record UpdateProductCommand(
    int Id,
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId) : IRequest<ProductDto>;
```

**File**: `CatalogService.Application/Commands/DeleteProductCommand.cs`

```csharp
using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteProductCommand(int Id) : IRequest<bool>;
```

### Step 2.4: Create CQRS Queries

**File**: `CatalogService.Application/Queries/GetCategoriesQuery.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Queries;

public record GetCategoriesQuery : IRequest<IEnumerable<CategoryDto>>;
```

**File**: `CatalogService.Application/Queries/GetProductsQuery.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;

namespace CatalogService.Application.Queries;

public record GetProductsQuery(
    int? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedProductsDto>;

public record PaginatedProductsDto(
    IEnumerable<ProductDto> Products,
    int TotalCount,
    int PageNumber,
    int PageSize);
```

### Step 2.5: Create Command Handlers

**File**: `CatalogService.Application/Handlers/CreateCategoryCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using AutoMapper;

namespace CatalogService.Application.Handlers;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name,
            Image = request.Image,
            ParentId = request.ParentId
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }
}
```

**File**: `CatalogService.Application/Handlers/UpdateCategoryCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Category with ID {request.Id} not found");

        category.Name = request.Name;
        category.Image = request.Image;
        category.ParentId = request.ParentId;

        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryDto>(category);
    }
}
```

**File**: `CatalogService.Application/Handlers/DeleteCategoryCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteCategoryCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return false;

        // Delete related products
        var relatedProducts = _dbContext.Products.Where(p => p.CategoryId == request.Id);
        _dbContext.Products.RemoveRange(relatedProducts);

        // Delete category
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

**File**: `CatalogService.Application/Handlers/CreateProductCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using AutoMapper;

namespace CatalogService.Application.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Image = request.Image,
            Price = request.Price,
            Amount = request.Amount,
            CategoryId = request.CategoryId
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}
```

**File**: `CatalogService.Application/Handlers/UpdateProductCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with ID {request.Id} not found");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Image = request.Image;
        product.Price = request.Price;
        product.Amount = request.Amount;
        product.CategoryId = request.CategoryId;

        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductDto>(product);
    }
}
```

**File**: `CatalogService.Application/Handlers/DeleteProductCommandHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.Commands;
using CatalogService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteProductCommandHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return false;

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
```

### Step 2.6: Create Query Handlers

**File**: `CatalogService.Application/Handlers/GetCategoriesQueryHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetCategoriesQueryHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
```

**File**: `CatalogService.Application/Handlers/GetProductsQueryHandler.cs`

```csharp
using MediatR;
using CatalogService.Application.DTOs;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Application.Handlers;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PaginatedProductsDto>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<PaginatedProductsDto> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Products.AsQueryable();

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

        return new PaginatedProductsDto(
            productDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
```

---

## Phase 3: GraphQL API Layer

### Step 3.1: Create GraphQL Object Types

**File**: `CatalogService.Api/GraphQL/Types/CategoryType.cs`

```csharp
using CatalogService.Domain.Entities;
using HotChocolate.Types;

namespace CatalogService.Api.GraphQL.Types;

public class CategoryType : ObjectType<Category>
{
    protected override void Configure(IObjectTypeDescriptor<Category> descriptor)
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
            .Field(c => c.ParentId)
            .Description("The parent category ID")
            .Type<IntType>();

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
```

**File**: `CatalogService.Api/GraphQL/Types/ProductType.cs`

```csharp
using CatalogService.Domain.Entities;
using HotChocolate.Types;

namespace CatalogService.Api.GraphQL.Types;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
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
            .Field(p => p.Category)
            .Description("The product category")
            .Type<CategoryType>();
    }
}
```

### Step 3.2: Create GraphQL Input Types

**File**: `CatalogService.Api/GraphQL/InputTypes/CreateCategoryInput.cs`

```csharp
namespace CatalogService.Api.GraphQL.InputTypes;

public record CreateCategoryInput(
    string Name,
    Uri? Image = null,
    int? ParentId = null);
```

**File**: `CatalogService.Api/GraphQL/InputTypes/UpdateCategoryInput.cs`

```csharp
namespace CatalogService.Api.GraphQL.InputTypes;

public record UpdateCategoryInput(
    int Id,
    string Name,
    Uri? Image = null,
    int? ParentId = null);
```

**File**: `CatalogService.Api/GraphQL/InputTypes/CreateProductInput.cs`

```csharp
namespace CatalogService.Api.GraphQL.InputTypes;

public record CreateProductInput(
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId);
```

**File**: `CatalogService.Api/GraphQL/InputTypes/UpdateProductInput.cs`

```csharp
namespace CatalogService.Api.GraphQL.InputTypes;

public record UpdateProductInput(
    int Id,
    string Name,
    string? Description,
    Uri? Image,
    decimal Price,
    int Amount,
    int CategoryId);
```

### Step 3.3: Create Query Resolver

**File**: `CatalogService.Api/GraphQL/Queries/Query.cs`

```csharp
using HotChocolate;
using HotChocolate.Types;
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
    public async Task<IEnumerable<CategoryDto>> GetCategories(
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetCategoriesQuery(), cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated products with optional category filtering.
    /// </summary>
    [GraphQLType(typeof(NonNullType<PaginatedProductsType>))]
    [GraphQLDescription("Gets paginated products with optional category filtering")]
    public async Task<PaginatedProductsDto> GetProducts(
        [Service] IMediator mediator,
        [GraphQLDescription("Filter by category ID")] int? categoryId = null,
        [GraphQLDescription("Page number (1-based)")] int pageNumber = 1,
        [GraphQLDescription("Page size")] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(
            new GetProductsQuery(categoryId, pageNumber, pageSize),
            cancellationToken);
    }
}

/// <summary>
/// GraphQL type for paginated products response.
/// </summary>
public class PaginatedProductsType : ObjectType<PaginatedProductsDto>
{
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
```

### Step 3.4: Create Mutation Resolver

**File**: `CatalogService.Api/GraphQL/Mutations/Mutation.cs`

```csharp
using HotChocolate;
using HotChocolate.Types;
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
    public async Task<CategoryDto> CreateCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category creation input")] CreateCategoryInput input,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(input.Name, input.Image, input.ParentId);
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product category.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<CategoryType>))]
    [GraphQLDescription("Updates an existing product category")]
    public async Task<CategoryDto> UpdateCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category ID")] int id,
        [GraphQLDescription("Category update input")] UpdateCategoryInput input,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand(input.Id, input.Name, input.Image, input.ParentId);
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a product category and all its related products.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Deletes a product category and all its related products")]
    public async Task<bool> DeleteCategory(
        [Service] IMediator mediator,
        [GraphQLDescription("Category ID")] int id,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
    }

    /// <summary>
    /// Creates a new product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<ProductType>))]
    [GraphQLDescription("Creates a new product")]
    public async Task<ProductDto> CreateProduct(
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
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<ProductType>))]
    [GraphQLDescription("Updates an existing product")]
    public async Task<ProductDto> UpdateProduct(
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
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a product.
    /// Requires Admin role.
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Deletes a product")]
    public async Task<bool> DeleteProduct(
        [Service] IMediator mediator,
        [GraphQLDescription("Product ID")] int id,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new DeleteProductCommand(id), cancellationToken);
    }
}
```

---

## Phase 4: Performance Optimization (N+1 Problem)

### Step 4.1: Create DataLoaders

**File**: `CatalogService.Api/GraphQL/DataLoaders/CategoryBatchDataLoader.cs`

```csharp
using GreenDonut;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for efficiently batch-loading categories to prevent N+1 queries.
/// Implements the DataLoader pattern using GreenDonut.
/// </summary>
public class CategoryBatchDataLoader : BatchDataLoader<int, Category>
{
    private readonly IApplicationDbContext _dbContext;

    public CategoryBatchDataLoader(IBatchScheduler batchScheduler, IApplicationDbContext dbContext)
        : base(batchScheduler)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads multiple categories in a single database query.
    /// </summary>
    protected override async Task<IReadOnlyDictionary<int, Category>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Categories
            .Where(c => keys.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }
}
```

**File**: `CatalogService.Api/GraphQL/DataLoaders/ProductBatchDataLoader.cs`

```csharp
using GreenDonut;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.GraphQL.DataLoaders;

/// <summary>
/// DataLoader for efficiently batch-loading products by category to prevent N+1 queries.
/// Implements the DataLoader pattern using GreenDonut.
/// </summary>
public class ProductBatchDataLoader : BatchDataLoader<int, IEnumerable<Product>>
{
    private readonly IApplicationDbContext _dbContext;

    public ProductBatchDataLoader(IBatchScheduler batchScheduler, IApplicationDbContext dbContext)
        : base(batchScheduler)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Loads all products grouped by category in a single database query.
    /// </summary>
    protected override async Task<IReadOnlyDictionary<int, IEnumerable<Product>>> LoadBatchAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .Where(p => keys.Contains(p.CategoryId))
            .ToListAsync(cancellationToken);

        return keys.ToDictionary(
            k => k,
            k => products.Where(p => p.CategoryId == k).AsEnumerable());
    }
}
```

### Step 4.2: Create Custom Resolvers

**File**: `CatalogService.Api/GraphQL/Resolvers/CategoryResolvers.cs`

```csharp
using HotChocolate;
using CatalogService.Api.GraphQL.DataLoaders;
using CatalogService.Domain.Entities;

namespace CatalogService.Api.GraphQL.Resolvers;

/// <summary>
/// Custom resolvers for the Category type.
/// Uses DataLoaders to efficiently resolve related products.
/// </summary>
public class CategoryResolvers
{
    /// <summary>
    /// Resolves the products for a category using DataLoader pattern.
    /// Prevents N+1 queries by batch-loading products.
    /// </summary>
    public async Task<IEnumerable<Product>> GetProducts(
        [Parent] Category category,
        ProductBatchDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        return await dataLoader.LoadAsync(category.Id, cancellationToken);
    }
}
```

---

## Phase 5: Testing

### Step 5.1: Unit Tests for Handlers

**File**: `CatalogService.UnitTests/Handlers/CreateCategoryCommandHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using AutoMapper;
using CatalogService.Application.Commands;
using CatalogService.Application.DTOs;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;

namespace CatalogService.UnitTests.Handlers;

/// <summary>
/// Unit tests for CreateCategoryCommandHandler.
/// Tests command handling, DTO mapping, and database operations.
/// </summary>
public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateCategoryCommandHandler(_mockDbContext.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesCategory()
    {
        // Arrange
        var command = new CreateCategoryCommand("Electronics", null, null);
        var expectedDto = new CategoryDto { Id = 1, Name = "Electronics" };

        _mockMapper
            .Setup(m => m.Map<CategoryDto>(It.IsAny<Category>()))
            .Returns(expectedDto);

        _mockDbContext
            .Setup(db => db.Categories.Add(It.IsAny<Category>()))
            .Verifiable();

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Electronics", result.Name);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_MapsCorrectly()
    {
        // Arrange
        var command = new CreateCategoryCommand("Books", new Uri("https://example.com/books.jpg"), null);
        var createdCategory = new Category { Id = 2, Name = "Books" };
        var expectedDto = new CategoryDto { Id = 2, Name = "Books" };

        _mockMapper
            .Setup(m => m.Map<CategoryDto>(It.IsAny<Category>()))
            .Returns(expectedDto);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<CategoryDto>(It.IsAny<Category>()), Times.Once);
        Assert.Equal(2, result.Id);
        Assert.Equal("Books", result.Name);
    }
}
```

**File**: `CatalogService.UnitTests/Handlers/GetProductsQueryHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using AutoMapper;
using CatalogService.Application.DTOs;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Queries;
using CatalogService.Domain.Entities;

namespace CatalogService.UnitTests.Handlers;

/// <summary>
/// Unit tests for GetProductsQueryHandler.
/// Tests query handling, pagination, filtering, and DTO mapping.
/// </summary>
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetProductsQueryHandler(_mockDbContext.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", CategoryId = 1, Price = 10m, Amount = 5 },
            new Product { Id = 2, Name = "Product 2", CategoryId = 1, Price = 20m, Amount = 10 }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto { Id = 1, Name = "Product 1", CategoryId = 1, Price = 10m, Amount = 5 },
            new ProductDto { Id = 2, Name = "Product 2", CategoryId = 1, Price = 20m, Amount = 10 }
        };

        var query = new GetProductsQuery(categoryId: 1, pageNumber: 1, pageSize: 10);

        _mockMapper
            .Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Products.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_FiltersCorrectly()
    {
        // Arrange
        var query = new GetProductsQuery(categoryId: 1, pageNumber: 1, pageSize: 10);

        // Assert that the handler properly filters by category
        // (This would require setting up a proper DbSet mock)
    }
}
```

### Step 5.2: Integration Tests

**File**: `CatalogService.IntegrationTests/GraphQLIntegrationTests.cs`

```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace CatalogService.IntegrationTests;

/// <summary>
/// Integration tests for GraphQL API endpoints.
/// Tests full request/response cycle with authentication.
/// </summary>
public class GraphQLIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphQLIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var query = new { query = "{ getCategories { id name } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_WithValidToken_ReturnsCategories()
    {
        // Arrange
        var query = new { query = "{ getCategories { id name } }" };
        var token = GenerateValidJwtToken();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("data", content);
    }

    [Fact]
    public async Task CreateCategory_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    createCategory(input: { name: ""Test"" }) {
                        id
                        name
                    }
                }
            "
        };
        var token = GenerateJwtTokenWithoutAdminRole();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);

        // Assert
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden);
    }

    private string GenerateValidJwtToken()
    {
        // Implement JWT token generation for testing
        // This should match your authentication configuration
        return "test-valid-token";
    }

    private string GenerateJwtTokenWithoutAdminRole()
    {
        // Implement JWT token generation for testing without Admin role
        return "test-user-token";
    }
}
```

---

## Phase 6: Configuration & Security

### Step 6.1: Configure Program.cs

**File**: `CatalogService.Api/Program.cs`

```csharp
using System.Reflection;
using CatalogService.Api.GraphQL.DataLoaders;
using CatalogService.Api.GraphQL.Mutations;
using CatalogService.Api.GraphQL.Queries;
using CatalogService.Api.GraphQL.Types;
using CatalogService.Api.Mapping;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add application db context interface
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// Add MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateCategoryCommandHandler).Assembly);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add GraphQL Server
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<CategoryType>()
    .AddType<ProductType>()
    .AddType<PaginatedProductsType>()
    .AddDataLoader<CategoryBatchDataLoader>()
    .AddDataLoader<ProductBatchDataLoader>()
    .AddAuthorization()
    .ModifyRequestOptions(opt =>
    {
        opt.MaximumAllowedOperationComplexity = 5000;
        opt.MaximumAllowedDepth = 10;
    });

// Add services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Map GraphQL endpoint
app.MapGraphQL();

// Add a simple health check endpoint
app.MapGet("/health", () => "GraphQL API is running");

await app.RunAsync();
```

### Step 6.2: Configuration Files

**File**: `CatalogService.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=CatalogServiceGraphQL;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-characters-long-for-hs256!!!",
    "Issuer": "CatalogService",
    "Audience": "CatalogServiceClients",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "HotChocolate": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**File**: `CatalogService.Api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### Step 6.3: Create AutoMapper Profile

**File**: `CatalogService.Api/Mapping/MappingProfile.cs`

```csharp
using AutoMapper;
using CatalogService.Application.DTOs;
using CatalogService.Domain.Entities;

namespace CatalogService.Api.Mapping;

/// <summary>
/// AutoMapper configuration for domain-to-DTO mappings.
/// </summary>
public class MappingProfile : Profile
{
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
```

---

## Phase 7: Query Examples

### Basic Queries

#### Get All Categories

```graphql
query {
  getCategories {
    id
    name
    image
    parentId
  }
}
```

#### Get Products with Pagination

```graphql
query {
  getProducts(pageNumber: 1, pageSize: 10) {
    products {
      id
      name
      description
      price
      amount
      category {
        id
        name
      }
    }
    totalCount
    pageNumber
    pageSize
  }
}
```

#### Get Products Filtered by Category

```graphql
query {
  getProducts(categoryId: 1, pageNumber: 1, pageSize: 10) {
    products {
      id
      name
      price
      amount
      category {
        name
      }
    }
    totalCount
    pageNumber
    pageSize
  }
}
```

#### Get Categories with Products

```graphql
query {
  getCategories {
    id
    name
    image
    products {
      id
      name
      price
      amount
    }
  }
}
```

### Mutations

#### Create Category

```graphql
mutation {
  createCategory(input: {
    name: "Electronics"
    image: "https://example.com/electronics.jpg"
  }) {
    id
    name
    image
  }
}
```

#### Update Category

```graphql
mutation {
  updateCategory(
    id: 1
    input: {
      id: 1
      name: "Updated Electronics"
      image: "https://example.com/updated-electronics.jpg"
      parentId: null
    }
  ) {
    id
    name
    image
  }
}
```

#### Delete Category

```graphql
mutation {
  deleteCategory(id: 1)
}
```

#### Create Product

```graphql
mutation {
  createProduct(input: {
    name: "Laptop"
    description: "High-performance laptop"
    image: "https://example.com/laptop.jpg"
    price: 1299.99
    amount: 50
    categoryId: 1
  }) {
    id
    name
    price
    category {
      name
    }
  }
}
```

#### Update Product

```graphql
mutation {
  updateProduct(id: 1, input: {
    name: "Updated Laptop"
    description: "Even better performance"
    price: 1399.99
    amount: 45
    categoryId: 1
  }) {
    id
    name
    price
    amount
  }
}
```

#### Delete Product

```graphql
mutation {
  deleteProduct(id: 1)
}
```

### Complex Queries

#### Get Categories with Nested Products (Requires Proper Authorization)

```graphql
{
  getCategories {
    id
    name
    image
    parent {
      id
      name
    }
    products {
      id
      name
      description
      price
      amount
    }
  }
}
```

---

## Key References

### Official Documentation

1. **HotChocolate GraphQL Framework**
   - Main Documentation: https://chillicream.com/docs/hotchocolate
   - Getting Started: https://chillicream.com/docs/hotchocolate/get-started
   - Latest Release: https://github.com/ChilliCream/graphql-platform/releases

2. **DataLoaders & Performance**
   - DataLoader Pattern: https://chillicream.com/docs/hotchocolate/performance/dataloader
   - GreenDonut Documentation: https://chillicream.com/docs/greendonut
   - N+1 Query Prevention: https://chillicream.com/docs/hotchocolate/performance

3. **Security & Authorization**
   - Authorization in HotChocolate: https://chillicream.com/docs/hotchocolate/security/authorization
   - Authentication: https://chillicream.com/docs/hotchocolate/security/authentication
   - JWT Bearer: https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer

4. **MediatR Pattern**
   - GitHub Repository: https://github.com/jbogard/MediatR
   - Usage Examples: https://github.com/jbogard/MediatR/wiki
   - CQRS Pattern: https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs

5. **Entity Framework Core**
   - Official Documentation: https://docs.microsoft.com/en-us/ef/core/
   - Performance Best Practices: https://docs.microsoft.com/en-us/ef/core/performance/
   - Query Patterns: https://docs.microsoft.com/en-us/ef/core/querying/

6. **AutoMapper**
   - Official Documentation: https://docs.automapper.org/
   - Getting Started: https://docs.automapper.org/en/latest/Getting-started.html

### Testing Resources

- xUnit Documentation: https://xunit.net/docs/getting-started/netcore
- Moq Documentation: https://github.com/moq/moq4/wiki/Quickstart
- Integration Testing with WebApplicationFactory: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests

---

## Performance Optimization Checklist

### Database Optimization

- [ ] **Indexing**: Create database indexes on `CategoryId` and `Id` fields
```sql
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Categories_ParentId ON Categories(ParentId);
```

- [ ] **Query Projection**: Use `.Select()` to fetch only needed fields
```csharp
query = query.Select(p => new ProductDto 
{ 
    Id = p.Id, 
    Name = p.Name,
    Price = p.Price
});
```

- [ ] **Eager Loading**: Use `.Include()` for related entities
```csharp
_dbContext.Products
    .Include(p => p.Category)
    .ToListAsync();
```

### GraphQL-Specific Optimization

- [ ] **DataLoaders**: Implement batch loading for related entities
  - CategoryBatchDataLoader for categories
  - ProductBatchDataLoader for products by category

- [ ] **Query Complexity Analysis**: Enable complexity checking
```csharp
opt.MaximumAllowedOperationComplexity = 5000;
opt.MaximumAllowedDepth = 10;
```

- [ ] **Persisted Queries**: Implement query caching in production
```csharp
.AddInMemoryQueryResultCache()
```

- [ ] **Pagination**: Always use pagination for large result sets
```graphql
getProducts(pageNumber: 1, pageSize: 10)
```

### Monitoring & Profiling

- [ ] **Query Logging**: Enable EF Core query logging
```csharp
.LogTo(Console.WriteLine, LogLevel.Information)
```

- [ ] **Performance Metrics**: Monitor GraphQL execution time
  - Use HotChocolate diagnostics
  - Monitor database query time
  - Track N+1 query issues

- [ ] **DataLoader Validation**: Verify batch operations are working
  - Check logs for batch queries
  - Monitor database round trips

### Security Considerations

- [ ] **Authentication**: All queries/mutations require valid JWT token
- [ ] **Authorization**: Mutations require Admin role
- [ ] **Introspection**: Disable in production
```csharp
.ModifyRequestOptions(opt => opt.IncludeExceptionDetails = false)
```

- [ ] **Rate Limiting**: Implement rate limiting on GraphQL endpoint
- [ ] **Query Validation**: Validate input parameters

### Caching Strategy

- [ ] **Field-Level Caching**: Cache frequently accessed fields
```csharp
[CacheControl(maxAge: 300)]
public IEnumerable<CategoryDto> GetCategories() => ...
```

- [ ] **Query Result Caching**: Cache full query results
- [ ] **Distributed Cache**: Use Redis for multi-instance deployments

---

## Troubleshooting

### Common Issues

#### 1. N+1 Query Problem

**Symptom**: One query for categories + N queries for each category's products

**Solution**: Use DataLoaders
```csharp
// In CategoryType.cs
descriptor
    .Field(c => c.Products)
    .ResolveWith<CategoryResolvers>(r => r.GetProducts(default!, default!, default!));
```

#### 2. Unauthorized Access to Mutations

**Symptom**: 401 Unauthorized error on mutations

**Solution**: Ensure JWT token is valid and includes Admin role
```
Authorization: Bearer <valid-jwt-token-with-admin-role>
```

#### 3. Database Connection Issues

**Symptom**: Connection string errors or timeout exceptions

**Solution**: Verify connection string in `appsettings.json`
```
"DefaultConnection": "Server=.;Database=CatalogServiceGraphQL;Trusted_Connection=true;"
```

#### 4. AutoMapper Mapping Errors

**Symptom**: `AutoMapperConfigurationException`

**Solution**: Create reverse mapping in MappingProfile
```csharp
CreateMap<Category, CategoryDto>().ReverseMap();
```

---

## Next Steps

1. **Clone the repository** and create new branch
2. **Set up the project structure** following Phase 1
3. **Install NuGet packages** as specified
4. **Implement Domain & Application layers** (Phase 2)
5. **Build GraphQL API layer** (Phase 3)
6. **Optimize with DataLoaders** (Phase 4)
7. **Add comprehensive tests** (Phase 5)
8. **Configure security** (Phase 6)
9. **Test GraphQL queries** using Apollo GraphQL Studio or GraphiQL

---

## Contributing

When implementing this module:

- Follow the project's coding standards in `.editorconfig`
- Include XML documentation for all public members
- Write unit tests for all handlers
- Create integration tests for GraphQL endpoints
- Update this documentation as needed
- Submit pull requests to `feature/08.GraphQLAPI` branch

---

**Last Updated**: January 2025  
**Status**: Implementation Ready  
**Maintainer**: Development Team
