﻿using ComposableAsync;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using NYoutubeDL;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader
{
    class Twitch
    {
        public static int timeToSearch = 0;
        HttpClient twitch = null;
        public Twitch()
        {
            // Build handler with RateLimit
            DelegatingHandler handler = TimeLimiter.GetFromMaxCountByInterval(750, TimeSpan.FromMinutes(1)).AsDelegatingHandler();
            twitch = new HttpClient(handler);
        }

        public async void ClipSearch(Discord clientPassthrough, IConfigurationRoot configuration)
        {
            // Set headers for the twitch api request
            twitch.DefaultRequestHeaders.Add("Client-ID", configuration.GetApiKey("Twitch"));
            twitch.DefaultRequestHeaders.Add("Authorization", configuration.GetApiKey("Twitch_OAuth"));
            timeToSearch = Convert.ToInt32(configuration.GetSettings("searchTime"));
            DateTime? fiveMinutesAgo = null;
            // Initialise Database Class
            Database database = new Database(configuration.GetConnectionString("Clips"));
            try
            {
                // Get the number of archived clips
                await database.OpenDBConnection();
                ulong numberOfArchivedClips = await database.GetNumberOfArchivedClips();
                await database.CloseDBConnection();

                // If database is empty, then first run of the application, get all past clips
                if (numberOfArchivedClips == 0)
                {
                    await ConfigClipSearch(fiveMinutesAgo, database, configuration, clientPassthrough, true);
                }
                do
                {
                    ConfigClipSearch(fiveMinutesAgo, database, configuration, clientPassthrough, false);
                    await Task.Delay(timeToSearch * 60000);
                } while (true);
            }
            finally
            {
                // No matter what happens, close Database Connection
                await database.CloseDBConnection();
            }

        }

        private async Task ConfigClipSearch(DateTime? fiveMinutesAgo, Database database, IConfigurationRoot configuration, Discord discord, bool getAllClips)
        {       
            DateTime currentTime = DateTime.Now;
            if (fiveMinutesAgo == null)
            {
                fiveMinutesAgo = currentTime.AddMinutes(timeToSearch * -1);
            }
            DateTime past = (DateTime)fiveMinutesAgo;
            var counter = 0;
            TwitchClass streamTwitch = null;
            string finalDate = "";
            List<ClipInfo> clips = new List<ClipInfo>();
            string broadcasterId = configuration.GetSettings("Broadcaster_ID");
            do
            {
                if (counter > 0)
                {
                    currentTime = currentTime.AddMinutes(timeToSearch * -1);
                    past = past.AddMinutes(timeToSearch * -1);
                }
                counter++;
                string endDate = currentTime.ToString("yyyy") + "-" + currentTime.ToString("MM") + "-" + currentTime.ToString("dd") + "T" + currentTime.ToString("HH") + ":" + currentTime.ToString("mm") + ":" + currentTime.ToString("ss") + "Z";
                string date = past.ToString("yyyy") + "-" + past.ToString("MM") + "-" + past.ToString("dd") + "T" + past.ToString("HH") + ":" + past.ToString("mm") + ":" + past.ToString("ss") + "Z";
                int PaginationCounter = 0;
                do
                {

                    string url = "https://api.twitch.tv/helix/clips?broadcaster_id=" + broadcasterId + "&started_at=";
                    if (PaginationCounter == 0)
                    {
                        PaginationCounter++;
                        url = url + date + "&ended_at=" + endDate;
                    }
                    else
                    {
                        url = url + date + "&ended_at=" + endDate + "&after=" + streamTwitch.pagination.cursor;
                    }
                    string responseBody = await twitch.GetAsync(url).Result.Content.ReadAsStringAsync();
                    streamTwitch = JsonConvert.DeserializeObject<TwitchClass>(responseBody);
                    foreach (ClipInfo clip in streamTwitch.data)
                    {
                        clips.Add(clip);
                    }
                } while (streamTwitch.pagination.cursor != null);
                finalDate = endDate;
            } while (getAllClips && (currentTime.Month != 5 || currentTime.Day != 25 || currentTime.Year != 2016));
            // Filter out duplicates
            clips = clips.Distinct().ToList();
            Console.WriteLine("done getting clips " + finalDate);
            Console.WriteLine("Got " + clips.Count + " Clips");
            Console.WriteLine();
            string json = JsonConvert.SerializeObject(clips);
            // Existed to write results to file
            // using (StreamWriter file = File.CreateText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/clips/data.json"))
            // {
            //     JsonSerializer serializer = new JsonSerializer();
            //     //serialize object directly into file stream
            //     serializer.Serialize(file, clips);
            // }
            if (clips.Count > 0)
            {
                // Order from Oldest to Newest
                clips =  clips.OrderBy(d => d.created_at).ToList();
                await DownloadClips(discord, database, clips);
            }
        }

        public async Task DownloadClips(Discord discord, Database database , List<ClipInfo> clips)
        {
            YoutubeDL twitchDownload = new YoutubeDL();
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/clips";
            await database.OpenDBConnection();
            foreach (ClipInfo clip in clips)
            {
                string tempPath = path + "/video.mp4";
                try
                {
                    twitchDownload.Options.FilesystemOptions.Output = tempPath;
                    twitchDownload.VideoUrl = clip.url;
                    await twitchDownload.DownloadAsync();
                    await discord.UploadClipToDiscord(tempPath, clip);
                    
                    await database.ClipToDatabase(clip);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            await database.CloseDBConnection();
            Console.WriteLine("finished");
        }
    }
}
