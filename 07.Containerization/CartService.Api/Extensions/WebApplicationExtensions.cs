using CartService.Api.Middleware;

namespace CartService.Api.Extensions;

/// <summary>
/// <see cref="WebApplication"/> class extensions
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Extension method to add <see cref="TokenLoggingMiddleware"/> to pipeline
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseTokenLogging(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<TokenLoggingMiddleware>();
        return app;
    }
}