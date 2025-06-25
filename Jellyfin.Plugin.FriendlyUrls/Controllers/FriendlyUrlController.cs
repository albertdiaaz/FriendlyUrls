using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Library;
using FriendlyUrls.Data;
using FriendlyUrls.Services;

namespace FriendlyUrls.Controllers
{
    /// <summary>
    /// API controller for managing friendly URLs
    /// </summary>
    [ApiController]
    [Route("FriendlyUrls")]
    public class FriendlyUrlController : ControllerBase
    {
        private readonly IFriendlyUrlRepository _repository;
        private readonly IUrlGeneratorService _urlGenerator;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<FriendlyUrlController> _logger;

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
        /// Resolves a friendly URL to its original Jellyfin URL
        /// </summary>
        /// <param name="friendlyUrl">The friendly URL to resolve</param>
        /// <returns>Redirect to original URL or 404</returns>
        [HttpGet("resolve/{*friendlyUrl}")]
        public async Task<IActionResult> ResolveFriendlyUrl(string friendlyUrl)
        {
            try
            {
                var mapping = await _repository.GetByFriendlyUrlAsync($"/web/{friendlyUrl}");

                if (mapping == null)
                {
                    return NotFound();
                }

                // Update access statistics
                mapping.AccessCount++;
                mapping.LastAccessed = DateTime.UtcNow;
                await _repository.UpdateAsync(mapping);

                // Redirect to original URL
                return Redirect(mapping.OriginalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving friendly URL: {FriendlyUrl}", friendlyUrl);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Generates a friendly URL for a specific item
        /// </summary>
        /// <param name="itemId">The ID of the item to generate URL for</param>
        /// <returns>Generated friendly URL</returns>
        [HttpPost("generate/{itemId}")]
        public async Task<IActionResult> GenerateUrl(Guid itemId)
        {
            try
            {
                var item = _libraryManager.GetItemById(itemId);
                if (item == null)
                {
                    return NotFound();
                }

                var existingMapping = await _repository.GetByItemIdAsync(itemId);
                if (existingMapping != null)
                {
                    return Ok(new { friendlyUrl = existingMapping.FriendlyUrl });
                }

                var mapping = _urlGenerator.CreateMapping(item);
                if (mapping == null)
                {
                    return BadRequest("Cannot generate URL for this item type");
                }

                await _repository.SaveAsync(mapping);
                return Ok(new { friendlyUrl = mapping.FriendlyUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for item: {ItemId}", itemId);
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Gets all URL mappings
        /// </summary>
        /// <returns>List of all mappings</returns>
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
                return StatusCode(500);
            }
        }
    }
}