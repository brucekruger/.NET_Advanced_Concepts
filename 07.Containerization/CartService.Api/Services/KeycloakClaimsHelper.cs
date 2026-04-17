using System.Security.Claims;
using System.Text.Json;

namespace CartService.Api.Services;

/// <summary>
/// Helper class for extracting and transforming Keycloak JWT claims.
/// Provides reusable methods for role extraction from Keycloak's non-standard claim structure.
/// </summary>
public static class KeycloakClaimsHelper
{
    /// <summary>
    /// Extracts roles from the realm_access claim and adds them as standard role claims.
    /// </summary>
    public static void ExtractRolesFromRealmAccess(ClaimsIdentity claimsIdentity, Claim realmAccessClaim, ILogger? logger = null)
    {
        try
        {
            // realm_access claim typically contains JSON like: {"roles": ["Manager", "user"]}
            var realmAccessAsString = realmAccessClaim.Value;
            
            if (realmAccessAsString.Contains("roles"))
            {
                // Extract roles from JSON
                var roles = ExtractRolesFromJson(realmAccessAsString, logger);
                foreach (var role in roles)
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                    logger?.LogInformation("Added role from realm_access: {Role}", role);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract roles from realm_access claim");
        }
    }

    /// <summary>
    /// Extracts roles from the resource_access claim and adds them as standard role claims.
    /// </summary>
    public static void ExtractRolesFromResourceAccess(ClaimsIdentity claimsIdentity, Claim resourceAccessClaim, string clientId, ILogger? logger = null)
    {
        try
        {
            // resource_access claim typically contains JSON like: {"{clientId}": {"roles": ["Manager"]}}
            var resourceAccessAsString = resourceAccessClaim.Value;
            
            if (resourceAccessAsString.Contains(clientId) && resourceAccessAsString.Contains("roles"))
            {
                var roles = ExtractRolesFromJson(resourceAccessAsString, logger);
                foreach (var role in roles)
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
                    logger?.LogInformation("Added role from resource_access: {Role}", role);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract roles from resource_access claim");
        }
    }

    /// <summary>
    /// Extracts roles from a JSON string that contains a roles array.
    /// </summary>
    public static List<string> ExtractRolesFromJson(string jsonString, ILogger? logger = null)
    {
        var roles = new List<string>();

        try
        {
            using (var document = JsonDocument.Parse(jsonString))
            {
                var root = document.RootElement;
                
                // Look for roles array at any level
                if (root.TryGetProperty("roles", out var rolesArray))
                {
                    ExtractRolesFromArray(rolesArray, roles);
                }
                else
                {
                    // If roles not found at root, check all properties
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.TryGetProperty("roles", out var nestedRolesArray))
                        {
                            ExtractRolesFromArray(nestedRolesArray, roles);
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to parse JSON for roles extraction");
        }

        return roles;
    }

    /// <summary>
    /// Extracts individual role strings from a JSON array element.
    /// </summary>
    public static void ExtractRolesFromArray(JsonElement rolesArray, List<string> roles)
    {
        if (rolesArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in rolesArray.EnumerateArray())
            {
                if (role.ValueKind == JsonValueKind.String)
                {
                    var roleString = role.GetString();
                    if (!string.IsNullOrEmpty(roleString))
                    {
                        roles.Add(roleString);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts the client ID from standard JWT claims.
    /// Tries both 'aud' (audience) and 'azp' (authorized party) claims.
    /// </summary>
    public static string? GetClientIdFromToken(ClaimsIdentity claimsIdentity)
    {
        // Try to get client_id from aud claim or azp claim
        var audClaim = claimsIdentity.FindFirst("aud")?.Value 
                    ?? claimsIdentity.FindFirst("azp")?.Value;
        return audClaim;
    }
}
