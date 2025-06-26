using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Jellyfin.Plugin.FriendlyUrls.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FriendlyUrls.Data
{
    /// <summary>
    /// Interface for friendly URL repository operations.
    /// </summary>
    public interface IFriendlyUrlRepository
    {
        Task<FriendlyUrlMapping?> GetByFriendlyUrlAsync(string friendlyUrl);
        Task<FriendlyUrlMapping?> GetByItemIdAsync(Guid itemId);
        Task<IEnumerable<FriendlyUrlMapping>> GetAllAsync();
        Task SaveAsync(FriendlyUrlMapping mapping);
        Task UpdateAsync(FriendlyUrlMapping mapping);
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// JSON-based repository for managing friendly URL mappings.
    /// </summary>
    public class FriendlyUrlRepository : IFriendlyUrlRepository
    {
        private readonly string _dataPath;
        private readonly ILogger<FriendlyUrlRepository> _logger;
        private readonly object _lock = new object();

        public FriendlyUrlRepository(IApplicationPaths appPaths, ILogger<FriendlyUrlRepository> logger)
        {
            _dataPath = Path.Combine(appPaths.DataPath, "friendly-urls.json");
            _logger = logger;
        }

        /// <summary>
        /// Gets a mapping by its friendly URL.
        /// </summary>
        /// <param name="friendlyUrl">The friendly URL to search for.</param>
        /// <returns>Matching mapping or null.</returns>
        public async Task<FriendlyUrlMapping?> GetByFriendlyUrlAsync(string friendlyUrl)
        {
            var mappings = await LoadMappingsAsync();
            return mappings.FirstOrDefault(m => m.FriendlyUrl == friendlyUrl && m.IsActive);
        }

        /// <summary>
        /// Gets a mapping by its item ID.
        /// </summary>
        /// <param name="itemId">The item ID to search for.</param>
        /// <returns>Matching mapping or null.</returns>
        public async Task<FriendlyUrlMapping?> GetByItemIdAsync(Guid itemId)
        {
            var mappings = await LoadMappingsAsync();
            return mappings.FirstOrDefault(m => m.ItemId == itemId && m.IsActive);
        }

        /// <summary>
        /// Gets all URL mappings.
        /// </summary>
        /// <returns>Collection of all mappings.</returns>
        public async Task<IEnumerable<FriendlyUrlMapping>> GetAllAsync()
        {
            return await LoadMappingsAsync();
        }

        /// <summary>
        /// Saves a new mapping to the storage.
        /// </summary>
        /// <param name="mapping">The mapping to save.</param>
        public async Task SaveAsync(FriendlyUrlMapping mapping)
        {
            var mappings = (await LoadMappingsAsync()).ToList();
            mappings.Add(mapping);
            await SaveMappingsAsync(mappings);
        }

        /// <summary>
        /// Updates an existing mapping.
        /// </summary>
        /// <param name="mapping">The mapping to update.</param>
        public async Task UpdateAsync(FriendlyUrlMapping mapping)
        {
            var mappings = (await LoadMappingsAsync()).ToList();
            var index = mappings.FindIndex(m => m.Id == mapping.Id);
            if (index >= 0)
            {
                mapping.UpdatedAt = DateTime.UtcNow;
                mappings[index] = mapping;
                await SaveMappingsAsync(mappings);
            }
        }

        /// <summary>
        /// Deletes a mapping by its ID.
        /// </summary>
        /// <param name="id">The ID of the mapping to delete.</param>
        public async Task DeleteAsync(Guid id)
        {
            var mappings = (await LoadMappingsAsync()).ToList();
            mappings.RemoveAll(m => m.Id == id);
            await SaveMappingsAsync(mappings);
        }

        private async Task<List<FriendlyUrlMapping>> LoadMappingsAsync()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_dataPath))
                    {
                        return new List<FriendlyUrlMapping>();
                    }

                    var json = File.ReadAllText(_dataPath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<FriendlyUrlMapping>();
                    }

                    var mappings = JsonSerializer.Deserialize<List<FriendlyUrlMapping>>(json);
                    return mappings ?? new List<FriendlyUrlMapping>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading friendly URL mappings from {Path}", _dataPath);
                    return new List<FriendlyUrlMapping>();
                }
            }
        }

        private async Task SaveMappingsAsync(List<FriendlyUrlMapping> mappings)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(_dataPath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };

                        var json = JsonSerializer.Serialize(mappings, options);
                        File.WriteAllText(_dataPath, json);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving friendly URL mappings to {Path}", _dataPath);
                    }
                }
            });
        }
    }
}