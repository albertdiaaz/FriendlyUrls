using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Jellyfin.Plugin.FriendlyUrls.Data;
using Jellyfin.Plugin.FriendlyUrls.Services;

namespace Jellyfin.Plugin.FriendlyUrls
{
    /// <summary>
    /// Registers plugin services with Jellyfin's dependency injection container
    /// </summary>
    public class PluginServiceRegistrar : IPluginServiceRegistrator
    {
        /// <summary>
        /// Registers services required by the plugin
        /// </summary>
        /// <param name="serviceCollection">Service collection to register with</param>
        /// <param name="serverApplicationHost">Server application host</param>
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost serverApplicationHost)
        {
            serviceCollection.AddSingleton<IFriendlyUrlRepository, FriendlyUrlRepository>();
            serviceCollection.AddSingleton<SlugService>();
            serviceCollection.AddSingleton<IUrlGeneratorService, UrlGeneratorService>();
        }
    }
}