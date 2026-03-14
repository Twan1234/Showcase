using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Showcase.Infra;

public static class SecurityMiddlewareExtensions
{
    private static readonly string[] AllowedHttpMethods = { "GET", "POST", "HEAD", "OPTIONS" };

    private static readonly string[] BlockedPathFragments =
    {
        "/.git", "/.svn", ".ds_store", "thumbs.db", "/.env"
    };

    private static readonly string[] StaticPathPrefixes =
    {
        "/css/", "/js/", "/lib/", "/static/", "/Identity/"
    };

    public static IApplicationBuilder UseAllowedMethodsOnly(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var method = context.Request.Method.ToUpperInvariant();
            if (Array.IndexOf(AllowedHttpMethods, method) < 0)
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                context.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Invalid method: {Method} {Path}", context.Request.Method, context.Request.Path);
                return;
            }
            await next();
        });
    }

    public static IApplicationBuilder UseShowcaseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = (context.Request.Path.Value ?? "").Replace('\\', '/');

            if (IsBlockedPath(path))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            SetSecurityHeaders(context.Response, path);
            SetCacheHeaders(context.Response, path);
            context.Response.OnStarting(() => OnResponseStarting(context, path));

            await next();
        });
    }

    private static bool IsBlockedPath(string path)
    {
        var lower = path.ToLowerInvariant();
        return BlockedPathFragments.Any(f => lower.Contains(f));
    }

    private static void SetSecurityHeaders(HttpResponse response, string path)
    {
        response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' https://www.google.com https://www.gstatic.com; " +
            "style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; frame-ancestors 'self'; " +
            "base-uri 'self'; form-action 'self' https://api.web3forms.com; " +
            "connect-src 'self' https://api.web3forms.com https://www.google.com";
        response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    private static void SetCacheHeaders(HttpResponse response, string path)
    {
        if (path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            return;
        if (StaticPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return;

        response.Headers["Cache-Control"] = "no-store, no-cache, private";
        response.Headers["Pragma"] = "no-cache";
    }

    private static Task OnResponseStarting(HttpContext context, string path)
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        var contentType = context.Response.ContentType ?? "";

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var baseContentType = contentType.Split(';')[0].Trim();
            var extension = baseContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0 ? "api.json"
                : baseContentType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0 ? "api.xml" : "api.bin";
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{extension}\"";
        }

        if (string.IsNullOrEmpty(contentType))
            context.Response.Headers["Content-Type"] = "application/octet-stream";
        else if (!contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase) &&
                 (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                  contentType.Contains("+xml", StringComparison.OrdinalIgnoreCase) ||
                  contentType.StartsWith("application/xml", StringComparison.OrdinalIgnoreCase)))
            context.Response.Headers["Content-Type"] = contentType.TrimEnd() + "; charset=utf-8";

        return Task.CompletedTask;
    }
}
