using System;

namespace FriendlyUrls.Models
{
    /// <summary>
    /// Represents a mapping between a friendly URL and original Jellyfin URL
    /// </summary>
    public class FriendlyUrlMapping
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public string FriendlyUrl { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public int AccessCount { get; set; }
        public DateTime? LastAccessed { get; set; }
    }
}