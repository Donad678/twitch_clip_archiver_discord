using Discord;
using Discord.WebSocket;
using NYoutubeDL;
using System;
using System.Collections.Generic;
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
        public async Task StartDiscordBot(string token, ulong serverId, ulong channelId)
        {
            client = new DiscordSocketClient();
            client.Log += Log;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            server = client.GetGuild(serverId) as SocketGuild;
            channel = server.GetChannel(channelId) as SocketTextChannel;
        }

        public async Task UploadClipToDiscord(string filepath, ClipInfo clip)
        {
            await channel.SendFileAsync(filepath, clip.title);
        }

        public async Task StopDiscordBot()
        {
            await client.StopAsync();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
