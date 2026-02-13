using System.IdentityModel.Tokens.Jwt;

namespace CartService.Api.Middleware;

/// <summary>
/// Middleware for logging token details
/// </summary>
public class TokenLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor for <see cref="TokenLoggingMiddleware"/>
    /// </summary>
    /// <param name="next"></param>
    /// <param name="loggerFactory"></param>
    public TokenLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<TokenLoggingMiddleware>();
    }

    /// <summary>
    /// Invocation of the middleware to log token details if present in the request
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var token))
        {
            try
            {
                var bearerToken = token.ToString().Replace("Bearer ", string.Empty);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(bearerToken);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type.Equals("sub"))?.Value ?? "Unknown";
                var userName = jwtToken.Claims.FirstOrDefault(c => c.Type.Equals("preferred_username"))?.Value ?? "Unknown";
                var roles = jwtToken.Claims.Where(c => c.Type.Equals("realm_access")).Select(c => c.Value);

                _logger.LogInformation("Token Access - UserID: {UserId}, UserName: {UserName}, Roles: {Roles} Path: {Path}, Method: {Method}, Time: {Time}",
                    userId, userName, string.Join(',', roles), context.Request.Path, context.Request.Method, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to parse JWT token: {Exception}", ex.Message);
            }
        }

        await _next(context);
    }
}