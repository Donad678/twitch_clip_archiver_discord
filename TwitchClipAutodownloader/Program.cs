﻿using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Timers;
using System;
using System.Collections.Generic;

namespace TwitchClipAutodownloader
{
    class Program
    {
        private static Discord discord = null;
        private static Logging log = null;
        private static Twitch twitch = null;
        private static bool skipCheck = false;
        public static bool SkipCheck
        {
            get
            {
                return skipCheck;
            }
            set
            {
                skipCheck = value;
            }
        }
        public static Twitch Twitch
        {
            get
            {
                return twitch;
            }
        }
        public static Logging Logging
        {
            get
            {
                return log;
            }
        }
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
            log = new Logging();
            discord = new Discord();
            await discord.StartDiscordBot(configuration.GetApiKey("Bot_Key"), ulong.Parse(configuration.GetSettings("Discord_Server")), ulong.Parse(configuration.GetSettings("Discord_Channel")));            
            twitch = new Twitch(configuration);
            Timer timer = new Timer();
            timer.Elapsed += async (sender, e) => Timer_Elapsed(sender, e);
            timer.Interval = 180000;
            timer.AutoReset = true;
            timer.Start();
            await Task.Delay(-1);
        }
        private async Task Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (Program.SkipCheck == false)
                    {
                        Program.SkipCheck = true;
                        twitch.ClipSearch(discord);
                    }
                    else
                    {
                        Logging.Log("Check Skipped");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(ex.Message);
                }
            });           
        }
    }

}
