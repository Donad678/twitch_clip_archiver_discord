using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TwitchClipAutodownloader
{
    class Logging
    {
        private static string path;
        private object _lock = new object();
        public Logging()
        {
            path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/logs/log";
        }

        public void Log(string messages)
        {
            List<string> msg = new List<string>();
            msg.Add(messages);
            Log(msg);
        }

        public void Log(List<string> messages)
        {
            lock (_lock)
            {
                string localPath = path + "_" + DateTime.UtcNow.ToString("yyyyMMdd") + ".txt";
                if (!File.Exists(localPath))
                {
                    FileStream stream = File.Create(localPath);
                    stream.Close();
                }
                DateTime currentTime = DateTime.UtcNow;
                foreach (string msg in messages)
                {
                    Console.WriteLine(msg);
                }
                using (StreamWriter writer = File.AppendText(localPath))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine("--- Log Entry ---");
                    writer.WriteLine($"{currentTime.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                    writer.WriteLine("---");
                    foreach (string message in messages)
                    {
                        writer.WriteLine($"{message}");
                        writer.WriteLine("---");
                    }
                    writer.WriteLine("--- End of Entry ---");
                }
            }

        }
    }
}
