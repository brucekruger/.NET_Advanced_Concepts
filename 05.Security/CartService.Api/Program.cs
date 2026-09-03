using CartService.Api.Middleware;
using CartService.Api.Services;
using CartService.Application.Interfaces;
using CartService.Infrastructure.Data;
using CartService.Infrastructure.Messaging;
using CartService.Infrastructure.Messaging.Configuration;
using CartService.Infrastructure.Messaging.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RabbitMQ.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using CartService.Api.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CartService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Authentication (same as CatalogService)
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var authSettings = builder.Configuration.GetSection("Authentication");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.Authority = authSettings["Authority"];
                options.Audience = authSettings["Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,  // Disabled for development
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authSettings["Authority"]
                };
            });

        builder.Services.AddAuthorization();

        // Add Keycloak claims transformation to map realm_access.roles to standard role claims
        builder.Services.AddScoped<IClaimsTransformation, CartService.Api.Services.KeycloakClaimsTransformation>();

        // Add services to the container.
        builder.Services.AddControllers();

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
                    "http://localhost:5064"   // CartService HTTP
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
                Title = "Cart Service API V1",
                Version = "v1",
                Description = "An API for managing shopping carts - V1"
            });

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Cart Service API V2",
                Version = "v2",
                Description = "An enhanced API for managing shopping carts - V2"
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

        builder.Services.AddScoped<ICartRepository, CartRepository>(sp =>
        {
            var connectionString = builder.Configuration.GetConnectionString("LiteDB");
            return new CartRepository(connectionString ?? throw new InvalidOperationException("LiteDB connection string is not configured"));
        });

        // Configure LiteDB
        CartConfiguration.ConfigureMapping();

        builder.Services.AddScoped<ICartService, Infrastructure.Services.CartService>();

        // Configure RabbitMQ
        var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
        builder.Services.AddSingleton(rabbitMqSettings);

        // Register RabbitMQ Connection
        builder.Services.AddSingleton<IConnectionFactory>(sp => new ConnectionFactory
        {
            HostName = rabbitMqSettings.HostName,
            UserName = rabbitMqSettings.UserName,
            Password = rabbitMqSettings.Password,
            VirtualHost = rabbitMqSettings.VirtualHost,
            Port = rabbitMqSettings.Port,
            AutomaticRecoveryEnabled = true
        });

        builder.Services.AddSingleton<IConnection>(sp =>
        {
            var factory = sp.GetRequiredService<IConnectionFactory>();
            return factory.CreateConnectionAsync("CartService").Result;
        });

        // Register Message Consumer
        builder.Services.AddSingleton<IMessageConsumer, RabbitMqConsumer>();

        // Register Hosted Service for message consumer
        builder.Services.AddHostedService<MessageConsumerHostedService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart Service API V1");
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Cart Service API V2");
                options.RoutePrefix = string.Empty;
            });
        }
        else
        {
            app.UseHttpsRedirection();
        }
        
        app.UseCors("AllowLocalhost");

        // Add custom middleware BEFORE authentication
        app.UseTokenLogging();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}