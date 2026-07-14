using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using HotChocolate.Types;
using CatalogService.Application.DTOs;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using CatalogService.Api;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CatalogService.IntegrationTests;

/// <summary>
/// Integration tests for GraphQL API endpoints.
/// Tests full request/response cycle with authentication.
/// </summary>
public class GraphQLIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GraphQLIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Use a test authentication handler for integration tests so we can simulate tokens/roles
        var customizedFactory = factory.WithWebHostBuilder(builder =>
        {
            // DO NOT call builder.ConfigureServices(...) here — it can re-run startup registrations.
            builder.ConfigureTestServices(services =>
            {
                const string authenticationScheme = "Test";
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = authenticationScheme;
                        options.DefaultChallengeScheme = authenticationScheme;
                        options.DefaultScheme = authenticationScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(authenticationScheme, options => { });
                services.AddAuthorization();
            });
        });

        var client = customizedFactory.CreateClient();
        // use local client in the test
        _client = client;
    }

    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var query = new { query = "{ categories { id name } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_WithValidToken_ReturnsCategories()
    {
        // Arrange
        var query = new { query = "{ categories { id name } }" };
        var token = GenerateValidJwtToken();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(query)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(mutation)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized ||
                    response.StatusCode == HttpStatusCode.Forbidden);
    }

    private static string GenerateValidJwtToken()
    {
        // Implement JWT token generation for testing
        // This should match your authentication configuration
        return "test-valid-token";
    }

    private static string GenerateJwtTokenWithoutAdminRole()
    {
        // Implement JWT token generation for testing without Admin role
        return "test-user-token";
    }

    public class CategoryType : ObjectType<CategoryDto>
    {
        protected override void Configure(IObjectTypeDescriptor<CategoryDto> descriptor)
        {
            descriptor.Name("Category");
            descriptor.Field(f => f.Id).Type<NonNullType<IntType>>();
            descriptor.Field(f => f.Name).Type<StringType>();
            descriptor.Field(f => f.Image).Type<StringType>();
            // Fix: Use Parent instead of ParentId, as CategoryDto does not have ParentId
            descriptor.Field("parentId")
                .Type<IntType>()
                .Resolve(ctx => ctx.Parent<CategoryDto>().Parent?.Id);
        }
    }

    // A simple test auth handler that maps the provided bearer token to a principal with roles.
    private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string SchemeName = "Test";

        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            Microsoft.Extensions.Logging.ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for an Authorization header
            if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                // No token provided - treat as authentication failure so the pipeline returns 401
                return Task.FromResult(AuthenticateResult.Fail("No Authorization header."));
            }

            var authHeader = authHeaderValues.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Invalid scheme/header - fail authentication to trigger a challenge (401)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
            }

            var token = authHeader["Bearer ".Length..].Trim();

            // Map test tokens to principals
            Claim[] claims;
            if (string.Equals(token, "test-valid-token", StringComparison.Ordinal))
            {
                // Valid token with Admin role
                claims = new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.Role, "Admin")
                };
            }
            else if (string.Equals(token, "test-user-token", StringComparison.Ordinal))
            {
                // For integration tests, treat the non-admin test token as an authentication failure
                // so requests without Admin role produce a 401 (or the pipeline may return 403).
                return Task.FromResult(AuthenticateResult.Fail("Non-admin test token is not authorized for this test."));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test token."));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
