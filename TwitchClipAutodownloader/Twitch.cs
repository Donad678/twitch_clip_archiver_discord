using ComposableAsync;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using NYoutubeDL;
using RateLimiter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader
{
    class Twitch
    {
        private static object _lockObj = new object();
        private static List<ClipInfo> clips = new List<ClipInfo>();
        public static int timeToSearch = 0;
        public static HttpClient twitch = null;
        private IConfigurationRoot configuration = null;
        public Twitch(IConfigurationRoot config)
        {
            // Build handler with RateLimit
            DelegatingHandler handler = TimeLimiter.GetFromMaxCountByInterval(750, TimeSpan.FromMinutes(1)).AsDelegatingHandler();
            twitch = new HttpClient(handler);
            // Set headers for the twitch api request
            twitch.DefaultRequestHeaders.Add("Client-ID", config.GetApiKey("Twitch"));
            twitch.DefaultRequestHeaders.Add("Authorization", config.GetApiKey("Twitch_OAuth"));
            configuration = config;
        }
        /// <summary>
        /// Set up intervalls to search for clips
        /// </summary>
        /// <param name="clientPassthrough"></param>
        /// <param name="configuration"></param>
        public async void ClipSearch(Discord clientPassthrough)
        {            
            timeToSearch = Convert.ToInt32(configuration.GetSettings("searchTime"));
            // Initialise Database Class
            Database database = new Database(configuration.GetConnectionString("Clips"));
            try
            {

                TwitchClass import = await ImportJson();
                if (import != null)
                {
                    clips = import.data;
                    clips = clips.Distinct().ToList();
                    Console.WriteLine("Imported " + clips.Count + " clips");
                    clips = clips.OrderBy(d => d.created_at).ToList();
                    await DownloadClips(clientPassthrough, database, clips);
                    clips = new List<ClipInfo>();
                }

                // Get the number of archived clips
                await database.OpenDBConnection();
                int numberOfArchivedClips = await database.GetNumberOfArchivedClips();
                await database.CloseDBConnection();
                
                // If database is empty, then first run of the application, get all past clips
                if (numberOfArchivedClips == 0)
                {
                    await ConfigClipSearch(database, clientPassthrough, true, DateTime.UtcNow, false, import);
                    if (clips.Count > 0)
                    {
                        clips = clips.Distinct().ToList();
                        Console.WriteLine("Got " + clips.Count + " clips");                        
                        // Order from Oldest to Newest
                        clips = clips.OrderBy(d => d.created_at).ToList();
                        await DownloadClips(clientPassthrough, database, clips);
                    }
                    clips = new List<ClipInfo>();
                }
                try
                {
                    // Push it in the background
                    await Task.Run(async () =>
                    {
                        try
                        {
                            clips = new List<ClipInfo>();
                            DateTime currentTime = DateTime.UtcNow;
                            bool DoubleCheck = false;
                            // Check if the time is around midnight to make a test check if every clip of the day is archived
                            if ((currentTime.Hour == 23 && currentTime.Minute > 30) || (currentTime.Hour == 0 && currentTime.Minute < 1))
                            {
                                DoubleCheck = true;
                            }
                            Task work1 = ConfigClipSearch(database, clientPassthrough, false, currentTime.AddMinutes(-20), DoubleCheck);
                            Task work2 = ConfigClipSearch(database, clientPassthrough, false, currentTime.AddMinutes(-30), DoubleCheck);
                            Task work3 = ConfigClipSearch(database, clientPassthrough, false, currentTime.AddMinutes(-40), DoubleCheck);
                            // await ConfigClipSearch(database, configuration, clientPassthrough, false, currentTime);
                            await Task.WhenAll(work1, work2, work3);
                            if (clips.Count > 0)
                            {
                                clips = clips.Distinct().ToList();
                                Program.Logging.Log("Got " + clips.Count + " clips");
                                // Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss") + " Got " + clips.Count + " clips");
                                // Existed to write results to file
                                // using (StreamWriter file = File.CreateText(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/clips/import.json"))
                                // {
                                //     Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                                //     TwitchClass twitchClass = new TwitchClass()
                                //     {
                                //         data = clips,
                                //         pagination = null
                                //     };
                                //     //serialize object directly into file stream
                                //     serializer.Serialize(file, twitchClass);
                                // }
                                // Order from Oldest to Newest
                                clips = clips.OrderBy(d => d.created_at).ToList();
                                await DownloadClips(clientPassthrough, database, clips);
                            }
                            else
                            {
                                Program.Logging.Log("There were no Clips found");
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.Logging.Log(ex.Message);
                            Program.Logging.Log(ex.StackTrace);
                        }
                    });
                    // await Task.Delay(60000);
                    // do
                    // {
                    //     await Task.Delay(250);
                    // } while (DateTime.UtcNow.Minute != 0 && DateTime.UtcNow.Minute != 30);
                    // await Task.Delay(30 * 60000);
                }
                catch (Exception ex)
                {
                    Program.Logging.Log(ex.Message);
                }
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
        private async Task ConfigClipSearch(Database database, Discord discord, bool getAllClips, DateTime currentTime, bool wholeDay = false, TwitchClass import = null)
        {
            await Task.Run(async () =>
            {
                // DateTime currentTime = DateTime.UtcNow;
                // if (bypass != null)
                // {
                //     currentTime = bypass.Value.AddMinutes(-10);
                // }
                DateTime past = currentTime.AddMinutes(-30);
                var counter = 0;
                TwitchClass streamTwitch = null;
                int endDay = 30;
                int endMonth = 1;
                int endYear = 2019;
                string finalDate = "";
                // List<ClipInfo> clips = new List<ClipInfo>();
                if (getAllClips || import != null)
                {
                    if (import != null)
                    {
                        clips = import.data;
                        clips = clips.OrderByDescending(d => d.created_at).ToList();
                        ClipInfo newest = clips[0];
                        DateTime newEnd = newest.created_at.AddDays(-3);
                        endMonth = newEnd.Month;
                        endDay = newEnd.Day;
                        endYear = newEnd.Year;
                    }
                }
                string broadcasterId = configuration.GetSettings("Broadcaster_ID");
                double amountsOfRunningThrough = 1;
                if (timeToSearch > 30)
                {
                    amountsOfRunningThrough = Math.Ceiling(Convert.ToDouble(timeToSearch) / 30);
                }
                if (wholeDay)
                {
                    amountsOfRunningThrough = 96;
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
                        streamTwitch = JsonSerializer.Deserialize<TwitchClass>(responseBody);
                        foreach (ClipInfo clip in streamTwitch.data)
                        {
                            lock (_lockObj)
                            {
                                clips.Add(clip);
                            }
                        }
                        // Console.Clear();
                        // Console.WriteLine("Got " + clips.Count + " Clips");
                    } while (streamTwitch.pagination.cursor != null);
                    finalDate = endDate;
                    amountsOfRunningThrough--;
                    if (amountsOfRunningThrough == 0 && getAllClips == false)
                    {
                        break;
                    }
                    else if (getAllClips && (currentTime.Month == endMonth && currentTime.Day == endDay && currentTime.Year == endYear))
                    {
                        break;
                    }
                } while (true);
                // Filter out duplicates
                // lock (_lockObj)
                // {
                //     clips = clips.Distinct().ToList();
                // }
                // Console.WriteLine("done getting clips " + finalDate);
                // Console.WriteLine("Got " + clips.Count + " Clips");
                // Console.WriteLine();            
                // if (clips.Count > 0)
                // {
                //     // Order from Oldest to Newest
                //     // clips = clips.OrderBy(d => d.created_at).ToList();
                //     // await DownloadClips(discord, database, clips);
                // }
            });            
        }

        public async Task<TwitchClass> ImportJson()
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/import.json";
            if (File.Exists(path))
            {
                TwitchClass obj = JsonSerializer.Deserialize<TwitchClass>(File.ReadAllText(path));
                return obj;
            }
            return null;
        }

        public async Task DownloadClips(Discord discord, Database database, List<ClipInfo> clips)
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
                    catch (Exception ex)
                    {
                        Program.Logging.Log(ex.Message);
                        Program.Logging.Log(ex.StackTrace);
                    }
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
            Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss") + " finished");
        }
    }
}
