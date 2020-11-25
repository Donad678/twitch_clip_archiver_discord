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
        public Logging(string p)
        {
            path = p;
            if (!File.Exists(p))
            {
                FileStream stream = File.Create(p);
                stream.Close();
            }
        }

        public void Log(string message)
        {            
            lock (_lock)
            {
                DateTime currentTime = DateTime.UtcNow;
                Console.WriteLine(message);
                using (StreamWriter writer = File.AppendText(path))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine("Log Entry");
                    writer.WriteLine($"{currentTime.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                    writer.WriteLine($"{message}");
                    writer.WriteLine("---");
                }
            }
            
        }
    }
}
