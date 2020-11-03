using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipAutodownloader
{
    class Database
    {
        MySqlConnection connection = null;
        string connectionString = "";

        public Database(string passedConnectionString)
        {
            connectionString = passedConnectionString;
        }

        public async Task OpenDBConnection()
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        public async Task CloseDBConnection()
        {
            connection.Close();
        }

        public async Task ClipToDatabase(ClipInfo clip)
        {

        }

        public async Task<ulong> GetNumberOfArchivedClips()
        {
            return 0;
        }

        public async Task<ClipInfo> DatabaseToClip()
        {
            ClipInfo clip = new ClipInfo();
            
            return clip;
        }
    }
}
