using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchClipAutodownloader
{
    public static class IConfigurationRootExtensions
    {
        /// <summary>
        /// Get API Key form appsettings.json
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="key">Name of key</param>
        /// <returns>Key Value</returns>
        public static string GetApiKey(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key)
        {
            return configuration.GetSection("APIKeys")[key];
        }
        /// <summary>
        /// Get Settings from appsettings.json
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="key">Name of Setting</param>
        /// <returns>Value of Setting</returns>
        public static string GetSettings(this Microsoft.Extensions.Configuration.IConfiguration configuration, string key)
        {
            return configuration.GetSection("Settings")[key];
        }
    }
}
