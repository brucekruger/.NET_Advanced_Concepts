using CatalogService.Api.Interfaces;
using CatalogService.Api.Services;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Data;
using CatalogService.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.OpenApi;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CatalogService.Api;

public class Program
{
    public static IConfiguration? Configuration { get; private set; }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
        // Configure Swagger generation with API versions
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service API V1");
                options.RoutePrefix = string.Empty;
            });
            
            // In development, don't force HTTPS redirection - allow both HTTP and HTTPS
        }
        else
        {
            // In production, enforce HTTPS redirection
            app.UseHttpsRedirection();
        }
        
        // Use CORS middleware - must be before UseAuthorization
        app.UseCors("AllowLocalhost");
        
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}