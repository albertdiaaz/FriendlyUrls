using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FriendlyUrls.Configuration
{
    /// <summary>
    /// Plugin configuration settings
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            // Set default values
            EnableMovieUrls = true;
            EnableShowUrls = true;
            EnablePersonUrls = true;
            EnableCollectionUrls = true;
            EnableGenreUrls = true;
            EnableStudioUrls = true;
            BaseUrl = "/web";
            ForceHttps = false;
            AutoGenerateUrls = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether movie URLs are enabled.
        /// </summary>
        public bool EnableMovieUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether show URLs are enabled.
        /// </summary>
        public bool EnableShowUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether person URLs are enabled.
        /// </summary>
        public bool EnablePersonUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether collection URLs are enabled.
        /// </summary>
        public bool EnableCollectionUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether genre URLs are enabled.
        /// </summary>
        public bool EnableGenreUrls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether studio URLs are enabled.
        /// </summary>
        public bool EnableStudioUrls { get; set; }

        /// <summary>
        /// Gets or sets the base URL for friendly URLs.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force HTTPS.
        /// </summary>
        public bool ForceHttps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically generate URLs for new content.
        /// </summary>
        public bool AutoGenerateUrls { get; set; }
    }
}