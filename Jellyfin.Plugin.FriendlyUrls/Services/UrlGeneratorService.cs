using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Jellyfin.Plugin.FriendlyUrls.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FriendlyUrls.Services
{
    /// <summary>
    /// Interface for URL generation service.
    /// </summary>
    public interface IUrlGeneratorService
    {
        string? GenerateFriendlyUrl(BaseItem item);
        FriendlyUrlMapping? CreateMapping(BaseItem item);
    }

    /// <summary>
    /// Service responsible for generating friendly URLs for different media types.
    /// </summary>
    public class UrlGeneratorService : IUrlGeneratorService
    {
        private readonly SlugService _slugService;
        private readonly ILogger<UrlGeneratorService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlGeneratorService"/> class.
        /// </summary>
        /// <param name="slugService">The slug service.</param>
        /// <param name="logger">The logger.</param>
        public UrlGeneratorService(SlugService slugService, ILogger<UrlGeneratorService> logger)
        {
            _slugService = slugService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a friendly URL for the given media item.
        /// </summary>
        /// <param name="item">The media item to generate URL for.</param>
        /// <returns>Friendly URL string or null if not supported.</returns>
        public string? GenerateFriendlyUrl(BaseItem item)
        {
            if (item == null)
            {
                return null;
            }

            var config = Plugin.Instance?.Configuration;
            var baseUrl = config?.BaseUrl?.TrimEnd('/') ?? "/web";

            return item switch
            {
                Movie movie when config?.EnableMovieUrls == true =>
                    $"{baseUrl}/movie/{_slugService.CreateSlug(movie.Name ?? "")}-{movie.ProductionYear}",

                Series show when config?.EnableShowUrls == true =>
                    $"{baseUrl}/show/{_slugService.CreateSlug(show.Name ?? "")}-{show.ProductionYear}",

                Season season when config?.EnableShowUrls == true && season.Series != null =>
                    $"{baseUrl}/show/{_slugService.CreateSlug(season.Series.Name ?? "")}/season-{season.IndexNumber}",

                Episode episode when config?.EnableShowUrls == true && episode.Series != null =>
                    $"{baseUrl}/show/{_slugService.CreateSlug(episode.Series.Name ?? "")}/season-{episode.ParentIndexNumber}/episode-{episode.IndexNumber}",

                Person person when config?.EnablePersonUrls == true =>
                    $"{baseUrl}/person/{_slugService.CreateSlug(person.Name ?? "")}",

                BoxSet collection when config?.EnableCollectionUrls == true =>
                    $"{baseUrl}/collection/{_slugService.CreateSlug(collection.Name ?? "")}",

                Genre genre when config?.EnableGenreUrls == true =>
                    $"{baseUrl}/genre/{_slugService.CreateSlug(genre.Name ?? "")}",

                Studio studio when config?.EnableStudioUrls == true =>
                    $"{baseUrl}/studio/{_slugService.CreateSlug(studio.Name ?? "")}",

                _ => null
            };
        }

        /// <summary>
        /// Creates a complete URL mapping for the given item.
        /// </summary>
        /// <param name="item">The media item to create mapping for.</param>
        /// <returns>FriendlyUrlMapping object or null if not supported.</returns>
        public FriendlyUrlMapping? CreateMapping(BaseItem item)
        {
            if (item == null)
            {
                _logger.LogWarning("Cannot create mapping for null item");
                return null;
            }

            var friendlyUrl = GenerateFriendlyUrl(item);
            if (string.IsNullOrEmpty(friendlyUrl))
            {
                _logger.LogDebug("No friendly URL generated for item {ItemId} of type {ItemType}", item.Id, item.GetType().Name);
                return null;
            }

            // Generate original URL - simplified approach
            var originalUrl = $"/web/index.html#!/details?id={item.Id}";

            var mapping = new FriendlyUrlMapping
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                ItemType = item.GetType().Name,
                FriendlyUrl = friendlyUrl,
                OriginalUrl = originalUrl,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AccessCount = 0
            };

            _logger.LogDebug("Created mapping for item {ItemId}: {FriendlyUrl} -> {OriginalUrl}",
                item.Id, friendlyUrl, originalUrl);

            return mapping;
        }
    }
}