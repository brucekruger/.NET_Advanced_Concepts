using CatalogService.Api.Controllers;
using CatalogService.Api.Interfaces;
using CatalogService.Api.Models;
using Microsoft.AspNetCore.Mvc;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CatalogService.Api.Services;

/// <summary>
/// Default implementation of IHateoasLinkBuilder using IUrlHelper from the controller context.
/// Dynamically resolves the protocol (http or https) from the current HTTP request.
/// Uses inline nameof() expressions for controller/action names and HttpMethod for HTTP verbs to avoid magic strings.
/// </summary>
public class HateoasLinkBuilder : IHateoasLinkBuilder
{
    private readonly IUrlHelper _urlHelper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HateoasLinkBuilder(IUrlHelper urlHelper, IHttpContextAccessor httpContextAccessor)
    {
        _urlHelper = urlHelper ?? throw new ArgumentNullException(nameof(urlHelper));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public IEnumerable<LinkDto> BuildCategoryLinks(int categoryId)
    {
        var protocol = GetProtocol();
        const string categoryController = "Category";
        
        yield return new LinkDto(
            "self",
            _urlHelper.Action(nameof(CategoryController.Get), categoryController, new { id = categoryId }, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
        yield return new LinkDto(
            "update",
            _urlHelper.Action(nameof(CategoryController.Put), categoryController, new { id = categoryId }, protocol) ?? string.Empty,
            HttpMethod.Put.Method
        );
        yield return new LinkDto(
            "delete",
            _urlHelper.Action(nameof(CategoryController.Delete), categoryController, new { id = categoryId }, protocol) ?? string.Empty,
            HttpMethod.Delete.Method
        );
        yield return new LinkDto(
            "list",
            _urlHelper.Action(nameof(CategoryController.Get), categoryController, null, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
    }

    public IEnumerable<LinkDto> BuildProductLinks(int productId, int categoryId)
    {
        var protocol = GetProtocol();
        const string productController = "Product";
        const string categoryController = "Category";
        
        yield return new LinkDto(
            "self",
            _urlHelper.Action(nameof(ProductController.Get), productController, new { id = productId }, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
        yield return new LinkDto(
            "update",
            _urlHelper.Action(nameof(ProductController.Put), productController, new { id = productId }, protocol) ?? string.Empty,
            HttpMethod.Put.Method
        );
        yield return new LinkDto(
            "delete",
            _urlHelper.Action(nameof(ProductController.Delete), productController, new { id = productId }, protocol) ?? string.Empty,
            HttpMethod.Delete.Method
        );
        yield return new LinkDto(
            "category",
            _urlHelper.Action(nameof(CategoryController.Get), categoryController, new { id = categoryId }, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
        yield return new LinkDto(
            "list",
            _urlHelper.Action(nameof(ProductController.Get), productController, null, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
    }

    public IEnumerable<LinkDto> BuildCategoryCollectionLinks()
    {
        var protocol = GetProtocol();
        const string categoryController = "Category";
        
        yield return new LinkDto(
            "self",
            _urlHelper.Action(nameof(CategoryController.Get), categoryController, null, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
        yield return new LinkDto(
            "create",
            _urlHelper.Action(nameof(CategoryController.Post), categoryController, null, protocol) ?? string.Empty,
            HttpMethod.Post.Method
        );
    }

    public IEnumerable<LinkDto> BuildProductCollectionLinks()
    {
        var protocol = GetProtocol();
        const string productController = "Product";
        
        yield return new LinkDto(
            "self",
            _urlHelper.Action(nameof(ProductController.Get), productController, null, protocol) ?? string.Empty,
            HttpMethod.Get.Method
        );
        yield return new LinkDto(
            "create",
            _urlHelper.Action(nameof(ProductController.Post), productController, null, protocol) ?? string.Empty,
            HttpMethod.Post.Method
        );
    }

    /// <summary>
    /// Gets the protocol (http or https) from the current HTTP context.
    /// </summary>
    /// <returns>The protocol scheme ("http" or "https"), or "https" as default if context is unavailable.</returns>
    private string GetProtocol()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.Request == null)
        {
            return "https";
        }

        return httpContext.Request.IsHttps ? "https" : "http";
    }
}