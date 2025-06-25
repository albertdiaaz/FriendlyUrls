using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;
using FriendlyUrls.Models;

namespace FriendlyUrls.Services
{
    public interface IUrlGeneratorService
    {
        string? GenerateFriendlyUrl(BaseItem item);
        FriendlyUrlMapping? CreateMapping(BaseItem item);
    }

    /// <summary>
    /// Service responsible for generating friendly URLs for different media types
    /// </summary>
    public class UrlGeneratorService : IUrlGeneratorService
    {
        private readonly SlugService _slugService;
        private readonly ILibraryManager _libraryManager;

        public UrlGeneratorService(SlugService slugService, ILibraryManager libraryManager)
        {
            _slugService = slugService;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Generates a friendly URL for the given media item
        /// </summary>
        /// <param name="item">The media item to generate URL for</param>
        /// <returns>Friendly URL string or null if not supported</returns>
        public string? GenerateFriendlyUrl(BaseItem item)
        {
            var baseUrl = Plugin.Instance?.Configuration?.BaseUrl?.TrimEnd('/') ?? "/web";

            return item switch
            {
                Movie movie => $"{baseUrl}/movie/{_slugService.CreateSlug(movie.Name ?? "")}-{movie.ProductionYear}",
                Series show => $"{baseUrl}/show/{_slugService.CreateSlug(show.Name ?? "")}-{show.ProductionYear}",
                Season season when season.Series != null => $"{baseUrl}/show/{_slugService.CreateSlug(season.Series.Name ?? "")}/season-{season.IndexNumber}",
                Episode episode when episode.Series != null => $"{baseUrl}/show/{_slugService.CreateSlug(episode.Series.Name ?? "")}/season-{episode.ParentIndexNumber}/episode-{episode.IndexNumber}",
                Person person => $"{baseUrl}/person/{_slugService.CreateSlug(person.Name ?? "")}",
                BoxSet collection => $"{baseUrl}/collection/{_slugService.CreateSlug(collection.Name ?? "")}",
                Genre genre => $"{baseUrl}/genre/{_slugService.CreateSlug(genre.Name ?? "")}",
                Studio studio => $"{baseUrl}/studio/{_slugService.CreateSlug(studio.Name ?? "")}",
                _ => null
            };
        }

        /// <summary>
        /// Creates a complete URL mapping for the given item
        /// </summary>
        /// <param name="item">The media item to create mapping for</param>
        /// <returns>FriendlyUrlMapping object or null if not supported</returns>
        public FriendlyUrlMapping? CreateMapping(BaseItem item)
        {
            var friendlyUrl = GenerateFriendlyUrl(item);
            if (string.IsNullOrEmpty(friendlyUrl))
                return null;

            // Get ServerId from the item's InternalId (fallback approach)
            var serverId = GetServerIdFromItem(item);

            return new FriendlyUrlMapping
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                ItemType = item.GetType().Name,
                FriendlyUrl = friendlyUrl,
                OriginalUrl = $"/web/#/details?id={item.Id}&serverId={serverId}",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }

        /// <summary>
        /// Gets the server ID for the given item
        /// </summary>
        /// <param name="item">The media item</param>
        /// <returns>Server ID string</returns>
        private string GetServerIdFromItem(BaseItem item)
        {
            // Try to get server ID from various sources
            try
            {
                // Method 1: Try to get from library manager
                var library = _libraryManager.GetItemById(item.Id);
                if (library != null)
                {
                    // Return a default server ID or extract from context
                    return "default-server-id";
                }
            }
            catch
            {
                // Fallback to empty string
            }

            return "";
        }
    }
}