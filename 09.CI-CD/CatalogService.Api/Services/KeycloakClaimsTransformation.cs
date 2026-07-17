using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace CatalogService.Api.Services;

/// <summary>
/// Transforms Keycloak JWT claims to ASP.NET Core standard claims.
/// Maps realm_access.roles to the standard role claim for authorization to work properly.
/// </summary>
public class KeycloakClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<KeycloakClaimsTransformation> _logger;

    public KeycloakClaimsTransformation(ILogger<KeycloakClaimsTransformation> logger)
    {
        _logger = logger;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimsIdentity = principal.Identity as ClaimsIdentity;
        if (claimsIdentity == null)
        {
            return Task.FromResult(principal);
        }

        // Get client ID from configuration to build the resource_access path
        var clientId = KeycloakClaimsHelper.GetClientIdFromToken(claimsIdentity);

        // Try to extract roles from realm_access.roles
        var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            KeycloakClaimsHelper.ExtractRolesFromRealmAccess(claimsIdentity, realmAccessClaim, _logger);
        }

        // Try to extract roles from resource_access.{clientId}.roles
        if (!string.IsNullOrEmpty(clientId))
        {
            var resourceAccessClaim = claimsIdentity.FindFirst("resource_access");
            if (resourceAccessClaim != null)
            {
                KeycloakClaimsHelper.ExtractRolesFromResourceAccess(claimsIdentity, resourceAccessClaim, clientId, _logger);
            }
        }

        return Task.FromResult(principal);
    }
}