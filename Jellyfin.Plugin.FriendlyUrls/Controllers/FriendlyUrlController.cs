using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using Jellyfin.Plugin.FriendlyUrls.Data;
using Jellyfin.Plugin.FriendlyUrls.Services;

namespace Jellyfin.Plugin.FriendlyUrls.Controllers
{
    /// <summary>
    /// API controller for managing friendly URLs.
    /// </summary>
    [ApiController]
    [Route("FriendlyUrls")]
    public class FriendlyUrlController : ControllerBase
    {
        private readonly IFriendlyUrlRepository _repository;
        private readonly IUrlGeneratorService _urlGenerator;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<FriendlyUrlController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FriendlyUrlController"/> class.
        /// </summary>
        /// <param name="repository">The friendly URL repository.</param>
        /// <param name="urlGenerator">The URL generator service.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public FriendlyUrlController(
            IFriendlyUrlRepository repository,
            IUrlGeneratorService urlGenerator,
            ILibraryManager libraryManager,
            ILogger<FriendlyUrlController> logger)
        {
            _repository = repository;
            _urlGenerator = urlGenerator;
            _libraryManager = libraryManager;
            _logger = logger;
        }

        /// <summary>
        /// Resolves a friendly URL to its original Jellyfin URL.
        /// </summary>
        /// <param name="friendlyUrl">The friendly URL to resolve.</param>
        /// <returns>Redirect to original URL or 404.</returns>
        [HttpGet("resolve/{*friendlyUrl}")]
        public async Task<IActionResult> ResolveFriendlyUrl(string friendlyUrl)
        {
            try
            {
                _logger.LogInformation("Attempting to resolve friendly URL: {FriendlyUrl}", friendlyUrl);

                var mapping = await _repository.GetByFriendlyUrlAsync($"/web/{friendlyUrl}");

                if (mapping == null)
                {
                    _logger.LogWarning("No mapping found for friendly URL: {FriendlyUrl}", friendlyUrl);
                    return NotFound();
                }

                // Update access statistics
                mapping.AccessCount++;
                mapping.LastAccessed = DateTime.UtcNow;
                await _repository.UpdateAsync(mapping);

                _logger.LogInformation("Redirecting to original URL: {OriginalUrl}", mapping.OriginalUrl);

                // Redirect to original URL
                return Redirect(mapping.OriginalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving friendly URL: {FriendlyUrl}", friendlyUrl);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Generates a friendly URL for a specific item.
        /// </summary>
        /// <param name="itemId">The ID of the item to generate URL for.</param>
        /// <returns>Generated friendly URL.</returns>
        [HttpPost("generate/{itemId}")]
        public async Task<IActionResult> GenerateUrl(Guid itemId)
        {
            try
            {
                _logger.LogInformation("Generating URL for item: {ItemId}", itemId);

                var item = _libraryManager.GetItemById(itemId);
                if (item == null)
                {
                    _logger.LogWarning("Item not found: {ItemId}", itemId);
                    return NotFound("Item not found");
                }

                var existingMapping = await _repository.GetByItemIdAsync(itemId);
                if (existingMapping != null)
                {
                    _logger.LogInformation("Existing mapping found for item: {ItemId}", itemId);
                    return Ok(new { friendlyUrl = existingMapping.FriendlyUrl });
                }

                var mapping = _urlGenerator.CreateMapping(item);
                if (mapping == null)
                {
                    _logger.LogWarning("Cannot generate URL for item type: {ItemType}", item.GetType().Name);
                    return BadRequest("Cannot generate URL for this item type");
                }

                await _repository.SaveAsync(mapping);

                _logger.LogInformation("Generated friendly URL: {FriendlyUrl} for item: {ItemId}", mapping.FriendlyUrl, itemId);

                return Ok(new { friendlyUrl = mapping.FriendlyUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for item: {ItemId}", itemId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Gets all URL mappings.
        /// </summary>
        /// <returns>List of all mappings.</returns>
        [HttpGet("mappings")]
        public async Task<IActionResult> GetAllMappings()
        {
            try
            {
                var mappings = await _repository.GetAllAsync();
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mappings");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        /// <returns>Plugin status.</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "OK", plugin = "Friendly URLs", version = "1.0.0" });
        }
    }
}