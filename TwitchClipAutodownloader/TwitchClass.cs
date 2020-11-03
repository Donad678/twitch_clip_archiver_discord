using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TwitchClipAutodownloader
{

    public class ClipInfo
    {
        /// <summary>
        /// The Clip object
        /// </summary>
        public string id { get; set; }
        public string url { get; set; }
        public string embed_url { get; set; }
        public string broadcaster_id { get; set; }
        public string broadcaster_name { get; set; }
        public string creator_id { get; set; }
        public string creator_name { get; set; }
        public string video_id { get; set; }
        public string game_id { get; set; }
        public string language { get; set; }
        public string title { get; set; }
        public int view_count { get; set; }
        public DateTime created_at { get; set; }
        public string thumbnail_url { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as ClipInfo;

            if (item == null)
            {
                return false;
            }
            // Check if id of both objects equal
            return this.id.Equals(item.id);
        }

        public override int GetHashCode()
        {
            //Get the hash code from the id.
            return id.GetHashCode();
        }

    }

    public class Pagination
    {
        public string cursor { get; set; }
    }

    public class TwitchClass
    {
        // Object used for parsing Json to C#
        public List<ClipInfo> data { get; set; }
        public Pagination pagination { get; set; }
    }    

}