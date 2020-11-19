using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using NYoutubeDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader.DiscordCommands
{
    public class DiscordUpload : ModuleBase<SocketCommandContext>
    {
        private bool ignoreDB = false;
        [Command("reupload")]
        [Alias("reup", "re")]
        [Summary("Uploads missing clip to the bot")]
        public async Task ReUploadClip(string url)
        {
            SocketGuild server = Context.Client.GetGuild(ulong.Parse(Program.configuration.GetSettings("Discord_Server")));
            SocketGuildUser currUser = server.GetUser(Context.User.Id);
            if (currUser.GuildPermissions.ManageMessages == true || currUser.Id == ulong.Parse("231449605458362368"))
            {
                ignoreDB = true;
                await UploadClip(url);
                ignoreDB = false;
            }
            else
            {
                await Context.Channel.SendMessageAsync("You don't have the rights to do that");
            }
            
        }

        [Command("upload")]
        [Alias("up")]
        [Summary("Uploads missing clip to the bot")]
        public async Task UploadClip(string url)
        {
            //ulong.Parse(configuration.GetSettings("Discord_Server")), ulong.Parse(configuration.GetSettings("Discord_Channel")
            SocketGuild server = Context.Client.GetGuild(ulong.Parse(Program.configuration.GetSettings("Discord_Server")));
            // Get Channel to send clips to
            SocketTextChannel channel = server.GetChannel(ulong.Parse(Program.configuration.GetSettings("Discord_Channel"))) as SocketTextChannel;
            Database database = new Database(Program.configuration.GetConnectionString("Clips"));
            string clipId = "";
            if (url.StartsWith("clips.twitch.tv"))
            {
                clipId = url.Split("/")[1];
            }
            else if (url.StartsWith("http"))
            {
                clipId = url.Split("//")[1].Split("/")[1];
            }
            else
            {
                clipId = url;
            }
            if (clipId != "")
            {
                string TwitchApiUrl = "https://api.twitch.tv/helix/clips?id=" + clipId;                
                string responseBody = await Twitch.twitch.GetAsync(TwitchApiUrl).Result.Content.ReadAsStringAsync();
                TwitchClass streamTwitch = JsonSerializer.Deserialize<TwitchClass>(responseBody);
                if (streamTwitch.data.Count != 0)
                {
                    ClipInfo clip = streamTwitch.data[0];
                    if (clip.broadcaster_id == Program.configuration.GetSettings("Broadcaster_ID"))
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime oneDayAgo = currentTime.AddDays(-1);
                        if (clip.created_at < oneDayAgo)
                        {
                            YoutubeDL twitchDownload = new YoutubeDL();
                            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/clips";
                            await database.OpenDBConnection();
                            if (ignoreDB || (await database.CheckIfClipAlreadyExists(clip.id)) == false)
                            {
                                string tempPath = path + $"/{clip.id}.mp4";
                                try
                                {
                                    twitchDownload.Options.FilesystemOptions.Output = tempPath;
                                    twitchDownload.VideoUrl = clip.url;
                                    await twitchDownload.DownloadAsync();
                                    await channel.SendFileAsync(tempPath, "", false, CreateEmbed(clip));

                                    await database.ClipToDatabase(clip);
                                    await Context.Channel.SendMessageAsync("Clip uploaded successfully");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    EmbedBuilder error = new EmbedBuilder()
                                    {
                                        Description = ex.StackTrace
                                    };
                                    await Context.Channel.SendMessageAsync("There was an Exception, Please send the following Message to Donad_VR", false, error.Build());
                                }
                                finally
                                {
                                    if (File.Exists(tempPath))
                                    {
                                        File.Delete(tempPath);
                                    }
                                }                                
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync("Clip already exists");
                            }
                            await database.CloseDBConnection();
                        }
                        else
                        {
                            DateTime creation = clip.created_at.AddDays(1);
                            await Context.Channel.SendMessageAsync("Clip has to be older than 24 hours, you can request the clip at " + creation.ToString("dd.MM.yyyy hh:mm:ss tt") + " CET");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("The clip has to be from Sean_VR");
                    }
                    
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Clip not found");
                }                                
            }
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
