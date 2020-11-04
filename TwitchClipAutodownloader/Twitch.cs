using ComposableAsync;
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
        /// <summary>
        /// Set up intervalls to search for clips
        /// </summary>
        /// <param name="clientPassthrough"></param>
        /// <param name="configuration"></param>
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
                int numberOfArchivedClips = await database.GetNumberOfArchivedClips();
                await database.CloseDBConnection();

                // If database is empty, then first run of the application, get all past clips
                if (numberOfArchivedClips == 0)
                {
                    await ConfigClipSearch(database, configuration, clientPassthrough, true);
                }
                bool did24h = false;
                do
                {
                    // Check if the time is around midnight to make a test check if every clip of the day is archived
                    if ((DateTime.Now.Hour == 23 && DateTime.Now.Minute > 30) || (DateTime.Now.Hour == 0 && DateTime.Now.Minute < 1))
                    {                    
                        await ConfigClipSearch(database, configuration, clientPassthrough, false, true);
                    }
                    else
                    {
                        // Push it in the background
                        Task.Run(async () =>
                        {
                            await ConfigClipSearch(database, configuration, clientPassthrough, false);
                        });
                        
                    }
                    await Task.Delay(30 * 60000);

                } while (true);
            }
            finally
            {
                // No matter what happens, close Database Connection
                await database.CloseDBConnection();
            }

        }
        /// <summary>
        /// Get the clips from twitch
        /// </summary>
        /// <param name="fiveMinutesAgo"></param>
        /// <param name="database"></param>
        /// <param name="configuration"></param>
        /// <param name="discord"></param>
        /// <param name="getAllClips"></param>
        /// <param name="wholeDay"></param>
        /// <returns></returns>
        private async Task ConfigClipSearch(Database database, IConfigurationRoot configuration, Discord discord, bool getAllClips, bool wholeDay = false)
        {       
            DateTime currentTime = DateTime.Now;
            DateTime past = currentTime.AddMinutes(-30);
            var counter = 0;
            TwitchClass streamTwitch = null;
            string finalDate = "";
            List<ClipInfo> clips = new List<ClipInfo>();
            string broadcasterId = configuration.GetSettings("Broadcaster_ID");
            double amountsOfRunningThrough = 1;            
            if (timeToSearch > 30)
            {
                amountsOfRunningThrough = Math.Ceiling(Convert.ToDouble(timeToSearch) / 30);
            }
            if (wholeDay)
            {
                amountsOfRunningThrough = 48;
            }
            do
            {
                if (counter > 0)
                {
                    currentTime = currentTime.AddMinutes(-30);
                    past = past.AddMinutes(-30);
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
                amountsOfRunningThrough--;
                if (amountsOfRunningThrough == 0 && getAllClips == false)
                {
                    break;
                }
                else if (getAllClips && (currentTime.Month == 1 && currentTime.Day == 30 && currentTime.Year == 2019))
                {
                    break;
                }
            } while (true);
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
                if (!await database.CheckIfClipAlreadyExists(clip.id))
                {
                    string tempPath = path + $"/{clip.id}.mp4";
                    try
                    {
                        twitchDownload.Options.FilesystemOptions.Output = tempPath;
                        twitchDownload.VideoUrl = clip.url;
                        await twitchDownload.DownloadAsync();
                        await discord.UploadClipToDiscord(tempPath, clip);

                        await database.ClipToDatabase(clip);
                    }
                    catch { }
                    finally
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                }                
            }
            await database.CloseDBConnection();
            Console.WriteLine("finished");
        }
    }
}
