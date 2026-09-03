using CatalogService.Api.Interfaces;
using CatalogService.Api.Mapping;
using CatalogService.Api.Services;
using CatalogService.Application.Handlers;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using CatalogService.Infrastructure.Messaging;
using CatalogService.Infrastructure.Messaging.Configuration;
using CatalogService.Infrastructure.Messaging.Interfaces;
using CatalogService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RabbitMQ.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using CatalogService.Api.GraphQL.DataLoaders;
using CatalogService.Api.GraphQL.Mutations;
using CatalogService.Api.GraphQL.Queries;
using CatalogService.Api.GraphQL.Types;
using Path = System.IO.Path;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CatalogService.Api;

public class Program
{
    public static IConfiguration? Configuration { get; private set; }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add MediatR
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateCategoryCommandHandler).Assembly);
        });

        // Add AutoMapper
        // Explicitly specify the namespace for AddAutoMapper:
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

                
        // Add Authentication
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var authSettings = builder.Configuration.GetSection("Authentication");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Use HTTPS metadata for production when Authority is https
                var authority = authSettings["Authority"];
                options.RequireHttpsMetadata = !(authority?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ?? false);
                options.Authority = authority;
                options.Audience = authSettings["Audience"];

                var audience = authSettings["Audience"];
                // Disable built-in audience validation and perform a custom check in OnTokenValidated
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Allow specifying an explicit ValidIssuer in config, fall back to Authority
                    ValidIssuer = authSettings["ValidIssuer"] ?? authority,
                    // Map Keycloak claims to ASP.NET identity
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role
                };

                // Save token on successful authentication (useful for diagnostics)
                options.SaveToken = true;

                // Add events to log failures and support tokens via query string for GraphQL playgrounds
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        // Support passing access_token via query string for non-browser GraphQL clients / playgrounds
                        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(accessToken) && context.Request.Path.StartsWithSegments("/graphql"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authentication challenge: {Error} {Description}", context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                    ,
                    OnTokenValidated = context =>
                    {
                        try
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            // If no audience configured, accept
                            if (string.IsNullOrEmpty(audience))
                                return Task.CompletedTask;

                            var principal = context.Principal;
                            // Gather aud claims
                            var audClaims = principal.Claims.Where(c => c.Type == "aud").Select(c => c.Value).ToList();

                            // Check azp claim
                            var azp = principal.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;

                            bool matched = audClaims.Any(a => string.Equals(a, audience, StringComparison.Ordinal));

                            if (!matched && !string.IsNullOrEmpty(azp) && string.Equals(azp, audience, StringComparison.Ordinal))
                                matched = true;

                            if (!matched)
                            {
                                logger.LogWarning("Token audience/azp does not match expected audience '{Audience}'. aud: {Aud}, azp: {Azp}", audience, audClaims, azp);
                                context.Fail("Invalid audience");
                            }
                        }
                        catch (Exception ex)
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError(ex, "Error validating token audience in OnTokenValidated");
                            context.Fail("Audience validation error");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

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
            .AddAuthorization();

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

        // Add Keycloak claims transformation to map realm_access.roles to standard role claims
        builder.Services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformation>();

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        // Add CORS configuration
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            options.AddPolicy("AllowLocalhost", policy =>
            {
                policy.WithOrigins(
                    "http://localhost:5063"   // CatalogService HTTP
                )
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
        });

        // Add API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        // Configure Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Set the comments path for the Swagger JSON and UI
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Catalog Service API V1",
                Version = "v1",
                Description = "An API for managing categories and products - V1"
            });

            // Add JWT Bearer Security Definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid JWT token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json") // This extension method is in Microsoft.Extensions.Configuration.Json
            .Build();

        var connectionString = Configuration.GetConnectionString("MSSQL");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
        builder.Services.AddScoped<IRepository<Product>, ProductRepository>();
        builder.Services.AddScoped<ICatalogService<Category>, CategoryService>();
        builder.Services.AddScoped<ICatalogService<Product>, ProductService>();

        // Register HATEOAS link builder for Level 3 REST compliance
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IHateoasLinkBuilder>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var urlHelperFactory = sp.GetRequiredService<IUrlHelperFactory>();
            var httpContext = httpContextAccessor.HttpContext;
            
            if (httpContext?.GetRouteData() is not null)
            {
                var routeData = httpContext.GetRouteData();
                var actionContext = new ActionContext(httpContext, routeData, new ControllerActionDescriptor());
                var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
                return new HateoasLinkBuilder(urlHelper, httpContextAccessor);
            }
            
            throw new InvalidOperationException("Unable to create URL helper for HATEOAS link builder.");
        });

        // Register the message publisher implementation (you need to identify your implementation)
        builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        // Register the product event publisher
        builder.Services.AddScoped<ProductEventPublisher>();

        // Configure RabbitMQ Settings
        var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
        builder.Services.AddSingleton(rabbitMqSettings);

        // Register RabbitMQ Connection
        builder.Services.AddSingleton<IConnection>(sp =>
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqSettings.HostName,
                UserName = rabbitMqSettings.UserName,
                Password = rabbitMqSettings.Password,
                VirtualHost = rabbitMqSettings.VirtualHost,
                Port = rabbitMqSettings.Port
            };
            
            return connectionFactory.CreateConnectionAsync().Result;
        });
        
        var app = builder.Build();

        // Apply database migrations
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service API V1");
                options.RoutePrefix = string.Empty;
            });
        }
        else
        {
            app.UseHttpsRedirection();
        }
        
        app.UseCors("AllowAll");

        // Ensure authentication middleware runs so registered authentication schemes are invoked
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Map GraphQL endpoint and require authentication on the HTTP endpoint so unauthenticated
        // requests receive a 401/403 before GraphQL execution.
        app.MapGraphQL();//.RequireAuthorization();

        // Add a simple health check endpoint
        app.MapGet("/health", () => "GraphQL API is running");

        await app.RunAsync();
    }
}
