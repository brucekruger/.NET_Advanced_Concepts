namespace CatalogService.IntegrationTests;

/// <summary>
/// Extension methods for Type introspection in integration tests.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines if the given type is an anonymous type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is an anonymous type; otherwise, false.</returns>
    internal static bool IsAnonymousType(this Type type)
    {
        return type.Name.Contains("AnonymousType")
               && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
               && type.Attributes.HasFlag(System.Reflection.TypeAttributes.NotPublic);
    }
}
