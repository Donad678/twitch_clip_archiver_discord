using Discord;
using Discord.WebSocket;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader
{
    class Discord
    {
        DiscordSocketClient client = null;
        SocketGuild server = null;
        SocketTextChannel channel = null;
        /// <summary>
        /// Start the Discord bot, it needs to be online to send stuff
        /// </summary>
        /// <param name="token"></param>
        /// <param name="serverId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task StartDiscordBot(string token, ulong serverId, ulong channelId)
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            // Wait until bot is online to get the needed Channel
            do
            {
                await Task.Delay(500);
            } while (client.ConnectionState != ConnectionState.Connected);
            // Get Server to send clips to
            server = client.GetGuild(serverId) as SocketGuild;
            // Get Channel to send clips to
            channel = server.GetChannel(channelId) as SocketTextChannel;
        }
        /// <summary>
        /// Upload the Clip into the discord channel
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public async Task UploadClipToDiscord(string filepath, ClipInfo clip)
        {
            await channel.SendFileAsync(filepath, "", false, CreateEmbed(clip));
        }
        /// <summary>
        /// Shutdown the Bot
        /// </summary>
        /// <returns></returns>
        public async Task StopDiscordBot()
        {
            await client.StopAsync();
        }
        /// <summary>
        /// Log Method to Log the Bot events
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        /// <summary>
        /// Embed creator to build a better Message
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        private Embed CreateEmbed(ClipInfo clip)
        {
            EmbedBuilder clipInfo = new EmbedBuilder()
            {
                Description = clip.title
            };
            string creatorName = "Name is missing";
            if (clip.creator_name != "")
            {
                creatorName = clip.creator_name;
            }
            EmbedFieldBuilder field1 = new EmbedFieldBuilder()
            {
                Name = "Creator",
                Value = creatorName,
                IsInline = true
            };
            EmbedFieldBuilder field2 = new EmbedFieldBuilder()
            {
                Name = "Created at",
                Value = clip.created_at.ToString("dd.MM.yyyy"),
                IsInline = true
            };
            clipInfo.AddField(field1);
            clipInfo.AddField(field2);
            Embed finished = clipInfo.Build();
            return finished;
        }
    }
}
