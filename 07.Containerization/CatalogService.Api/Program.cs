using CatalogService.Api.Interfaces;
using CatalogService.Api.Services;
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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CatalogService.Api;

public class Program
{
    public static IConfiguration? Configuration { get; private set; }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Authentication
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var authSettings = builder.Configuration.GetSection("Authentication");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.Authority = authSettings["Authority"];
                // When set, fetch OIDC metadata/keys from an internally reachable
                // address (e.g. http://keycloak:8080) while still validating the
                // public issuer advertised to clients.
                var metadataAddress = authSettings["MetadataAddress"];
                if (!string.IsNullOrEmpty(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;
                }
                options.Audience = authSettings["Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,  // Disabled for development
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authSettings["ValidIssuer"] ?? authSettings["Authority"]
                };
            });

        builder.Services.AddAuthorization();

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

        // Use the host configuration so environment variables (e.g. ConnectionStrings__MSSQL
        // provided by docker-compose) override appsettings.json values.
        Configuration = builder.Configuration;

        var connectionString = builder.Configuration.GetConnectionString("MSSQL");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddHealthChecks();

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

        // Ensure the database exists. SQL Server in a container can take a while to
        // accept connections, so retry a few times before giving up.
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            const int maxAttempts = 12;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    dbContext.Database.EnsureCreated();
                    logger.LogInformation("Database initialized successfully.");
                    break;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{MaxAttempts}). Retrying in 5s...", attempt, maxAttempts);
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        // Configure the HTTP request pipeline.
        // Swagger is enabled in all environments so the containerized API can be
        // explored and tested via /swagger.
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service API V1");
            options.RoutePrefix = "swagger";
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        
        app.UseCors("AllowLocalhost");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();

        app.Run();
    }
}