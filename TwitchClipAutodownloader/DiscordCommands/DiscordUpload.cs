using Discord.Commands;
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
        [Command("upload")]
        [Summary("Uploads missing clip to the bot")]
        public async Task UploadClip(string url)
        {
            //https://clips.twitch.tv/PiliableJoyousSnailFUNgineer
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
                    if (clip.creator_id != Program.configuration.GetSettings("Broadcaster_ID"))
                    {
                        DateTime currentTime = DateTime.Now;
                        DateTime oneDayAgo = currentTime.AddDays(-1);
                        if (clip.created_at > oneDayAgo)
                        {
                            YoutubeDL twitchDownload = new YoutubeDL();
                            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/clips";
                            await Twitch.Database.OpenDBConnection();
                            if (!await Twitch.Database.CheckIfClipAlreadyExists(clip.id))
                            {
                                string tempPath = path + $"/{clip.id}.mp4";
                                try
                                {
                                    twitchDownload.Options.FilesystemOptions.Output = tempPath;
                                    twitchDownload.VideoUrl = clip.url;
                                    await twitchDownload.DownloadAsync();
                                    await Program.Discord.UploadClipToDiscord(tempPath, clip);

                                    await Twitch.Database.ClipToDatabase(clip);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                                finally
                                {
                                    if (File.Exists(tempPath))
                                    {
                                        File.Delete(tempPath);
                                    }
                                }
                                await Twitch.Database.CloseDBConnection();
                            }
                            else
                            {
                                await Context.Channel.SendMessageAsync("Clip already exists");
                            }
                        }
                        else
                        {
                            DateTime creation = clip.created_at.AddDays(1);
                            await Context.Channel.SendMessageAsync("Clip has to be older than 24 hours, you can request the clip at " + creation.ToString("dd.MM.yyyy HH:mm:ss"));
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
    }
}
