using System.Reflection;
using CartService.Application.Interfaces;
using CartService.Infrastructure.Data;
using CartService.Infrastructure.Messaging;
using CartService.Infrastructure.Messaging.Configuration;
using CartService.Infrastructure.Messaging.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using RabbitMQ.Client;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CartService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}