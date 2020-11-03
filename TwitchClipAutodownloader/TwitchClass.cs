using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TwitchClipAutodownloader
{

    public class ClipInfo
    {
        /// <summary>
        /// 
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

            return this.id.Equals(item.id);
        }

        public override int GetHashCode()
        {
            //Calculate the hash code for the product.
            return id.GetHashCode();
        }

    }

    public class Pagination
    {
        public string cursor { get; set; }
    }

    public class TwitchClass
    {
        public List<ClipInfo> data { get; set; }
        public Pagination pagination { get; set; }
    }    

}