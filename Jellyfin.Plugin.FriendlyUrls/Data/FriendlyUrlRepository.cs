using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using MediaBrowser.Common.Configuration;
using FriendlyUrls.Models;

namespace FriendlyUrls.Data
{
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
    /// Repository for managing friendly URL mappings in SQLite database
    /// </summary>
    public class FriendlyUrlRepository : IFriendlyUrlRepository
    {
        private readonly FriendlyUrlContext _context;

        public FriendlyUrlRepository(IApplicationPaths appPaths)
        {
            var dbPath = Path.Combine(appPaths.DataPath, "friendly-urls.db");
            _context = new FriendlyUrlContext(dbPath);
            _context.Database.EnsureCreated();
        }

        /// <summary>
        /// Gets a mapping by its friendly URL
        /// </summary>
        /// <param name="friendlyUrl">The friendly URL to search for</param>
        /// <returns>Matching mapping or null</returns>
        public async Task<FriendlyUrlMapping?> GetByFriendlyUrlAsync(string friendlyUrl)
        {
            return await _context.FriendlyUrls
                .FirstOrDefaultAsync(u => u.FriendlyUrl == friendlyUrl && u.IsActive);
        }

        /// <summary>
        /// Gets a mapping by its item ID
        /// </summary>
        /// <param name="itemId">The item ID to search for</param>
        /// <returns>Matching mapping or null</returns>
        public async Task<FriendlyUrlMapping?> GetByItemIdAsync(Guid itemId)
        {
            return await _context.FriendlyUrls
                .FirstOrDefaultAsync(u => u.ItemId == itemId && u.IsActive);
        }

        /// <summary>
        /// Gets all URL mappings
        /// </summary>
        /// <returns>Collection of all mappings</returns>
        public async Task<IEnumerable<FriendlyUrlMapping>> GetAllAsync()
        {
            return await _context.FriendlyUrls.ToListAsync();
        }

        /// <summary>
        /// Saves a new mapping to the database
        /// </summary>
        /// <param name="mapping">The mapping to save</param>
        public async Task SaveAsync(FriendlyUrlMapping mapping)
        {
            _context.FriendlyUrls.Add(mapping);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing mapping
        /// </summary>
        /// <param name="mapping">The mapping to update</param>
        public async Task UpdateAsync(FriendlyUrlMapping mapping)
        {
            mapping.UpdatedAt = DateTime.UtcNow;
            _context.FriendlyUrls.Update(mapping);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a mapping by its ID
        /// </summary>
        /// <param name="id">The ID of the mapping to delete</param>
        public async Task DeleteAsync(Guid id)
        {
            var mapping = await _context.FriendlyUrls.FindAsync(id);
            if (mapping != null)
            {
                _context.FriendlyUrls.Remove(mapping);
                await _context.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Entity Framework context for friendly URL mappings
    /// </summary>
    public class FriendlyUrlContext : DbContext
    {
        private readonly string _dbPath;

        public FriendlyUrlContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public DbSet<FriendlyUrlMapping> FriendlyUrls { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FriendlyUrlMapping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FriendlyUrl).IsUnique();
                entity.HasIndex(e => e.ItemId);
                entity.Property(e => e.FriendlyUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OriginalUrl).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.ItemType).IsRequired().HasMaxLength(50);
            });
        }
    }
}