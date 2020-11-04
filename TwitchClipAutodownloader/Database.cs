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
        /// <summary>
        /// Open Connection to the Database
        /// </summary>
        /// <returns></returns>
        public async Task OpenDBConnection()
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }
        /// <summary>
        /// Close the connection to the database
        /// </summary>
        /// <returns></returns>
        public async Task CloseDBConnection()
        {
            connection.Close();
        }

        /// <summary>
        /// Save the whole clip information into the database
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public async Task ClipToDatabase(ClipInfo clip)
        {
            using (MySqlCommand command = new MySqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "INSERT INTO " +
                    "clips " +
                    "(id, url, embed_url, broadcaster_id, broadcaster_name, creator_id, creator_name, video_id, game_id, language, title, view_count, created_at, thumbnail_url)" +
                    " VALUES " +
                    "(@id, @url, @embed_url, @broadcaster_id, @broadcaster_name, @creator_id, @creator_name, @video_id, @game_id, @language, @title, @view_count, @created_at, @thumbnail_url)";
                command.Parameters.AddWithValue("@id", clip.id);
                command.Parameters.AddWithValue("@url", clip.url);
                command.Parameters.AddWithValue("@embed_url", clip.embed_url);
                command.Parameters.AddWithValue("@broadcaster_id", clip.broadcaster_id);
                command.Parameters.AddWithValue("@broadcaster_name", clip.broadcaster_name);
                command.Parameters.AddWithValue("@creator_id", clip.creator_id);
                command.Parameters.AddWithValue("@creator_name", clip.creator_name);
                command.Parameters.AddWithValue("@video_id", clip.video_id);
                command.Parameters.AddWithValue("@game_id", clip.game_id);
                command.Parameters.AddWithValue("@language", clip.language);
                command.Parameters.AddWithValue("@title", clip.title);
                command.Parameters.AddWithValue("@view_count", clip.view_count);
                command.Parameters.AddWithValue("@created_at", clip.created_at);
                command.Parameters.AddWithValue("@thumbnail_url", clip.thumbnail_url);
                await command.ExecuteNonQueryAsync();
            }

        }
        /// <summary>
        /// Get The number of archived clips, needed to check if first run
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetNumberOfArchivedClips()
        {           
            int result = 0;
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM clips", connection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result = reader.GetInt32(0);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Checks if the clip already exists, needed for the one check run every 24 hours
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfClipAlreadyExists(string id)
        {
            bool clipExists = false;
            using (var cmd = new MySqlCommand($"SELECT * FROM clips WHERE `id`='{id}'", connection))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        clipExists = true;
                    }
                }
            }
            return clipExists;
        }
    }
}
