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
            do
            {
                await Task.Delay(500);
            } while (client.ConnectionState != ConnectionState.Connected);
            server = client.GetGuild(serverId) as SocketGuild;
            channel = server.GetChannel(channelId) as SocketTextChannel;
        }

        public async Task UploadClipToDiscord(string filepath, ClipInfo clip)
        {
            await channel.SendFileAsync(filepath, "", false, CreateEmbed(clip));
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

        private Embed CreateEmbed(ClipInfo clip)
        {
            EmbedBuilder clipInfo = new EmbedBuilder()
            {
                Description = clip.title
            };
            EmbedFieldBuilder field1 = new EmbedFieldBuilder()
            {
                Name = "Creator",
                Value = clip.creator_name,
                IsInline = true
            };
            EmbedFieldBuilder field2 = new EmbedFieldBuilder()
            {
                Name = "Created at",
                Value = clip.created_at,
                IsInline = true
            };
            clipInfo.AddField(field1);
            clipInfo.AddField(field2);
            Embed finished = clipInfo.Build();
            return finished;
        }
    }
}
