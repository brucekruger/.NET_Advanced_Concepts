using System.Net.Mime;

namespace CatalogService.Api.Models;

/// <summary>
/// Represents a hypermedia link for HATEOAS (Hypertext As The Engine Of Application State).
/// </summary>
public record LinkDto(
    string Rel,
    string Href,
    string Method,
    string ContentType = MediaTypeNames.Application.Json);
