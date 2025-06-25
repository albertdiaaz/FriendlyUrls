using MediaBrowser.Model.Plugins;

namespace FriendlyUrls.Configuration
{
    /// <summary>
    /// Plugin configuration settings
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableMovieUrls { get; set; } = true;
        public bool EnableShowUrls { get; set; } = true;
        public bool EnablePersonUrls { get; set; } = true;
        public bool EnableCollectionUrls { get; set; } = true;
        public bool EnableGenreUrls { get; set; } = true;
        public bool EnableStudioUrls { get; set; } = true;
        public string BaseUrl { get; set; } = "/web";
        public bool ForceHttps { get; set; } = false;
        public bool AutoGenerateUrls { get; set; } = true;
    }
}