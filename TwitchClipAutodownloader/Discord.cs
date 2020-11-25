using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TwitchClipAutodownloader
{
    class Discord
    {
        DiscordSocketClient client = null;
        private IServiceProvider services;
        private CommandService commands;
        ulong ServerId = 0;
        ulong ChannelId = 0;

        /// <summary>
        /// Start the Discord bot, it needs to be online to send stuff
        /// </summary>
        /// <param name="token"></param>
        /// <param name="serverId"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task StartDiscordBot(string token, ulong serverId, ulong channelId)
        {
            ServerId = serverId;
            ChannelId = channelId;
            DiscordSocketConfig configuration = new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = true
            };
            client = new DiscordSocketClient(configuration);
            client.Log += Log;            
            commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async
            });
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            await InstallCommandsAsync();
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
            // Wait until bot is online to get the needed Channel
            do
            {
                await Task.Delay(500);
            } while (client.ConnectionState != ConnectionState.Connected);
           
        }

        public async Task InstallCommandsAsync()
        {
            client.MessageReceived += Client_MessageReceived;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            SocketUserMessage message = arg as SocketUserMessage;
            if (message != null)
            {
                int argPos = 0;
                if (message.Channel.GetType().Name == "SocketDMChannel")
                {
                    if (message.Author != client.CurrentUser)
                    {
                        var context = new SocketCommandContext(client, message);
                        var result = await commands.ExecuteAsync(context, argPos, services);
                        if (!result.IsSuccess)
                        {
                            var errorstream = new EmbedBuilder()
                            {
                                Title = "ERROR",
                                Description = result.ErrorReason,
                                ThumbnailUrl = "https://i.imgur.com/2Bu19vk.png"
                            };
                            Embed test = errorstream.Build();
                            await context.Channel.SendMessageAsync("", false, test);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Upload the Clip into the discord channel
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        public async Task UploadClipToDiscord(string filepath, ClipInfo clip)
        {
            // Get Server to send clips to
            SocketGuild server = client.GetGuild(ServerId) as SocketGuild;
            // Get Channel to send clips to
            SocketTextChannel channel = server.GetChannel(ChannelId) as SocketTextChannel;
            IDisposable typing = channel.EnterTypingState();
            await channel.SendFileAsync(filepath, "", false, CreateEmbed(clip));
            typing.Dispose();
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
            Program.Logging.Log(msg.ToString());
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
