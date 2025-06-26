using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Jellyfin.Plugin.FriendlyUrls.Services;
using Jellyfin.Plugin.FriendlyUrls.Data;

namespace Jellyfin.Plugin.FriendlyUrls.Services
{
    /// <summary>
    /// Service that monitors library changes and automatically generates friendly URLs.
    /// </summary>
    public class LibraryMonitorService : IHostedService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUrlGeneratorService _urlGenerator;
        private readonly IFriendlyUrlRepository _repository;
        private readonly ILogger<LibraryMonitorService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMonitorService"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="urlGenerator">The URL generator service.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="logger">The logger.</param>
        public LibraryMonitorService(
            ILibraryManager libraryManager,
            IUrlGeneratorService urlGenerator,
            IFriendlyUrlRepository repository,
            ILogger<LibraryMonitorService> logger)
        {
            _libraryManager = libraryManager;
            _urlGenerator = urlGenerator;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string Name => "Friendly URLs Library Monitor";

        /// <summary>
        /// Starts the hosted service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting Friendly URLs Library Monitor...");

                // Subscribe to library events for new content
                _libraryManager.ItemAdded += OnItemAdded;
                _libraryManager.ItemUpdated += OnItemUpdated;

                _logger.LogInformation("Library monitor started successfully");

                // Generate URLs for existing content if AutoGenerateUrls is enabled
                if (Plugin.Instance?.Configuration?.AutoGenerateUrls == true)
                {
                    _ = Task.Run(async () => await GenerateUrlsForExistingContent(), cancellationToken);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start library monitor");
                return Task.FromException(ex);
            }
        }

        /// <summary>
        /// Stops the hosted service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _libraryManager.ItemAdded -= OnItemAdded;
                _libraryManager.ItemUpdated -= OnItemUpdated;
                _logger.LogInformation("Library monitor stopped");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping library monitor");
                return Task.FromException(ex);
            }
        }

        /// <summary>
        /// Handles new items being added to the library.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnItemAdded(object? sender, ItemChangeEventArgs e)
        {
            if (Plugin.Instance?.Configuration?.AutoGenerateUrls != true)
            {
                return;
            }

            try
            {
                await GenerateUrlForItem(e.Item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for newly added item {ItemId}", e.Item.Id);
            }
        }

        /// <summary>
        /// Handles items being updated in the library.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnItemUpdated(object? sender, ItemChangeEventArgs e)
        {
            if (Plugin.Instance?.Configuration?.AutoGenerateUrls != true)
            {
                return;
            }

            try
            {
                // Check if item already has a mapping
                var existingMapping = await _repository.GetByItemIdAsync(e.Item.Id);
                if (existingMapping == null)
                {
                    await GenerateUrlForItem(e.Item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking/generating URL for updated item {ItemId}", e.Item.Id);
            }
        }

        /// <summary>
        /// Generates URLs for all existing content in the library.
        /// </summary>
        private async Task GenerateUrlsForExistingContent()
        {
            try
            {
                _logger.LogInformation("Starting bulk URL generation for existing content...");

                var rootFolders = _libraryManager.GetUserRootFolder().Children;
                int processedCount = 0;
                int generatedCount = 0;

                foreach (var folder in rootFolders)
                {
                    var result = await ProcessFolderAndReturnCounts(folder, processedCount, generatedCount);
                    processedCount = result.processed;
                    generatedCount = result.generated;
                }

                _logger.LogInformation("Bulk URL generation completed. Processed: {ProcessedCount}, Generated: {GeneratedCount}",
                    processedCount, generatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk URL generation");
            }
        }

        /// <summary>
        /// Processes a folder and returns updated counts.
        /// </summary>
        /// <param name="item">The item to process.</param>
        /// <param name="currentProcessed">Current processed count.</param>
        /// <param name="currentGenerated">Current generated count.</param>
        /// <returns>Updated counts.</returns>
        private async Task<(int processed, int generated)> ProcessFolderAndReturnCounts(BaseItem item, int currentProcessed, int currentGenerated)
        {
            try
            {
                // Process the current item
                if (await GenerateUrlForItem(item))
                {
                    currentGenerated++;
                }
                currentProcessed++;

                // Log progress every 100 items
                if (currentProcessed % 100 == 0)
                {
                    _logger.LogInformation("Bulk generation progress: {ProcessedCount} processed, {GeneratedCount} generated",
                        currentProcessed, currentGenerated);
                }

                // Process children if this is a folder
                if (item is Folder folder)
                {
                    foreach (var child in folder.GetRecursiveChildren())
                    {
                        if (await GenerateUrlForItem(child))
                        {
                            currentGenerated++;
                        }
                        currentProcessed++;

                        // Log progress every 100 items
                        if (currentProcessed % 100 == 0)
                        {
                            _logger.LogInformation("Bulk generation progress: {ProcessedCount} processed, {GeneratedCount} generated",
                                currentProcessed, currentGenerated);
                        }
                    }
                }

                return (currentProcessed, currentGenerated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item {ItemId} during bulk generation", item.Id);
                return (currentProcessed, currentGenerated);
            }
        }

        /// <summary>
        /// Recursively processes a folder and its children.
        /// </summary>
        /// <param name="item">The folder item to process.</param>
        /// <param name="processedCount">Reference to the processed count.</param>
        /// <param name="generatedCount">Reference to the generated count.</param>
        private async Task ProcessFolderRecursively(BaseItem item, int processedCount, int generatedCount)
        {
            try
            {
                // Process the current item
                if (await GenerateUrlForItem(item))
                {
                    generatedCount++;
                }
                processedCount++;

                // Log progress every 100 items
                if (processedCount % 100 == 0)
                {
                    _logger.LogInformation("Bulk generation progress: {ProcessedCount} processed, {GeneratedCount} generated",
                        processedCount, generatedCount);
                }

                // Process children if this is a folder
                if (item is Folder folder)
                {
                    foreach (var child in folder.GetRecursiveChildren())
                    {
                        if (await GenerateUrlForItem(child))
                        {
                            generatedCount++;
                        }
                        processedCount++;

                        // Log progress every 100 items
                        if (processedCount % 100 == 0)
                        {
                            _logger.LogInformation("Bulk generation progress: {ProcessedCount} processed, {GeneratedCount} generated",
                                processedCount, generatedCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item {ItemId} during bulk generation", item.Id);
            }
        }

        /// <summary>
        /// Generates a URL for a specific item if it doesn't already exist.
        /// </summary>
        /// <param name="item">The item to generate URL for.</param>
        /// <returns>True if a URL was generated, false otherwise.</returns>
        private async Task<bool> GenerateUrlForItem(BaseItem item)
        {
            try
            {
                // Check if mapping already exists
                var existingMapping = await _repository.GetByItemIdAsync(item.Id);
                if (existingMapping != null)
                {
                    return false; // Already has a mapping
                }

                // Try to generate a new mapping
                var mapping = _urlGenerator.CreateMapping(item);
                if (mapping == null)
                {
                    return false; // Item type not supported or generation failed
                }

                // Save the mapping
                await _repository.SaveAsync(mapping);

                _logger.LogDebug("Generated friendly URL for {ItemType} '{ItemName}': {FriendlyUrl}",
                    item.GetType().Name, item.Name, mapping.FriendlyUrl);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for item {ItemId}", item.Id);
                return false;
            }
        }
    }
}