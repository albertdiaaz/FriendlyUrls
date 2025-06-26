using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.FriendlyUrls.Data;
using Jellyfin.Plugin.FriendlyUrls.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.FriendlyUrls.Controllers
{
    /// <summary>
    /// Controller that handles friendly URL routing without /web prefix.
    /// </summary>
    [ApiController]
    public class FriendlyUrlWebController : ControllerBase
    {
        private readonly IFriendlyUrlRepository _repository;
        private readonly IUrlGeneratorService _urlGenerator;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<FriendlyUrlWebController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FriendlyUrlWebController"/> class.
        /// </summary>
        /// <param name="repository">The friendly URL repository.</param>
        /// <param name="urlGenerator">The URL generator service.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public FriendlyUrlWebController(
            IFriendlyUrlRepository repository,
            IUrlGeneratorService urlGenerator,
            ILibraryManager libraryManager,
            ILogger<FriendlyUrlWebController> logger)
        {
            _repository = repository;
            _urlGenerator = urlGenerator;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <summary>
        /// Handles movie URLs like /movie/title-year
        /// </summary>
        /// <param name="movieSlug">The movie slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("movie/{movieSlug}")]
        public async Task<IActionResult> HandleMovieUrl(string movieSlug)
        {
            return await ResolveFriendlyUrl($"/movie/{movieSlug}");
        }

        /// <summary>
        /// Handles show URLs like /show/title-year
        /// </summary>
        /// <param name="showSlug">The show slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("show/{showSlug}")]
        public async Task<IActionResult> HandleShowUrl(string showSlug)
        {
            return await ResolveFriendlyUrl($"/show/{showSlug}");
        }

        /// <summary>
        /// Handles show season URLs like /show/title/season-1
        /// </summary>
        /// <param name="showSlug">The show slug.</param>
        /// <param name="seasonSlug">The season slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("show/{showSlug}/{seasonSlug}")]
        public async Task<IActionResult> HandleShowSeasonUrl(string showSlug, string seasonSlug)
        {
            return await ResolveFriendlyUrl($"/show/{showSlug}/{seasonSlug}");
        }

        /// <summary>
        /// Handles show episode URLs like /show/title/season-1/episode-1
        /// </summary>
        /// <param name="showSlug">The show slug.</param>
        /// <param name="seasonSlug">The season slug.</param>
        /// <param name="episodeSlug">The episode slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("show/{showSlug}/{seasonSlug}/{episodeSlug}")]
        public async Task<IActionResult> HandleShowEpisodeUrl(string showSlug, string seasonSlug, string episodeSlug)
        {
            return await ResolveFriendlyUrl($"/show/{showSlug}/{seasonSlug}/{episodeSlug}");
        }

        /// <summary>
        /// Handles person URLs like /person/name
        /// </summary>
        /// <param name="personSlug">The person slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("person/{personSlug}")]
        public async Task<IActionResult> HandlePersonUrl(string personSlug)
        {
            return await ResolveFriendlyUrl($"/person/{personSlug}");
        }

        /// <summary>
        /// Handles collection URLs like /collection/name
        /// </summary>
        /// <param name="collectionSlug">The collection slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("collection/{collectionSlug}")]
        public async Task<IActionResult> HandleCollectionUrl(string collectionSlug)
        {
            return await ResolveFriendlyUrl($"/collection/{collectionSlug}");
        }

        /// <summary>
        /// Handles genre URLs like /genre/name
        /// </summary>
        /// <param name="genreSlug">The genre slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("genre/{genreSlug}")]
        public async Task<IActionResult> HandleGenreUrl(string genreSlug)
        {
            return await ResolveFriendlyUrl($"/genre/{genreSlug}");
        }

        /// <summary>
        /// Handles studio URLs like /studio/name
        /// </summary>
        /// <param name="studioSlug">The studio slug.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("studio/{studioSlug}")]
        public async Task<IActionResult> HandleStudioUrl(string studioSlug)
        {
            return await ResolveFriendlyUrl($"/studio/{studioSlug}");
        }

        /// <summary>
        /// Common method to resolve friendly URLs
        /// </summary>
        /// <param name="friendlyUrl">The friendly URL to resolve.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        private async Task<IActionResult> ResolveFriendlyUrl(string friendlyUrl)
        {
            try
            {
                _logger.LogInformation("Attempting to resolve friendly URL: {FriendlyUrl}", friendlyUrl);

                var mapping = await _repository.GetByFriendlyUrlAsync(friendlyUrl);

                if (mapping == null)
                {
                    _logger.LogWarning("No mapping found for friendly URL: {FriendlyUrl}", friendlyUrl);
                    return NotFound($"Content not found for URL: {friendlyUrl}");
                }

                // Update access statistics
                mapping.AccessCount++;
                mapping.LastAccessed = DateTime.UtcNow;
                await _repository.UpdateAsync(mapping);

                _logger.LogInformation("Redirecting {FriendlyUrl} to {OriginalUrl}", friendlyUrl, mapping.OriginalUrl);

                // Use permanent redirect (301) for SEO benefits
                return RedirectPermanent(mapping.OriginalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving friendly URL: {FriendlyUrl}", friendlyUrl);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}