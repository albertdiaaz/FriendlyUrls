using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.FriendlyUrls.Data;

namespace Jellyfin.Plugin.FriendlyUrls.Middleware
{
    /// <summary>
    /// Middleware that intercepts friendly URLs and rewrites them to original URLs
    /// </summary>
    public class UrlRewriteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFriendlyUrlRepository _repository;
        private readonly ILogger<UrlRewriteMiddleware> _logger;

        public UrlRewriteMiddleware(
            RequestDelegate next,
            IFriendlyUrlRepository repository,
            ILogger<UrlRewriteMiddleware> logger)
        {
            _next = next;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Processes the HTTP request and rewrites friendly URLs
        /// </summary>
        /// <param name="context">HTTP context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Check if this is a friendly URL
            if (IsFriendlyUrl(path))
            {
                try
                {
                    var mapping = await _repository.GetByFriendlyUrlAsync(path);
                    if (mapping != null)
                    {
                        // Update access statistics
                        mapping.AccessCount++;
                        mapping.LastAccessed = DateTime.UtcNow;
                        await _repository.UpdateAsync(mapping);

                        // Rewrite the URL
                        context.Request.Path = mapping.OriginalUrl;
                        context.Request.QueryString = QueryString.Empty;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in URL rewrite middleware for path: {Path}", path);
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Determines if the given path is a friendly URL
        /// </summary>
        /// <param name="path">The URL path to check</param>
        /// <returns>True if it's a friendly URL pattern</returns>
        private bool IsFriendlyUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var segments = new[] { "/web/movie/", "/web/show/", "/web/person/",
                                 "/web/collection/", "/web/genre/", "/web/studio/" };

            foreach (var segment in segments)
            {
                if (path.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}