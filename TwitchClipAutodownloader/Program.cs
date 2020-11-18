using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace TwitchClipAutodownloader
{
    class Program
    {
        private static Discord discord = null;


        public static Discord Discord
        {
            get
            {
                return discord;
            }
        }

        public static readonly IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true, true).Build();
        static void Main(string[] args)
        {
            configuration.Reload();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            discord = new Discord();
            await discord.StartDiscordBot(configuration.GetApiKey("Bot_Key"), ulong.Parse(configuration.GetSettings("Discord_Server")), ulong.Parse(configuration.GetSettings("Discord_Channel")));            
            Twitch twObj = new Twitch();
            twObj.ClipSearch(discord, configuration);
            await Task.Delay(-1);
        }        

    }

}
