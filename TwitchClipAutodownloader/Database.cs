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
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();
            }
            catch (Exception ex)
            {
                Program.Logging.Log(ex.Message);
                Environment.Exit(0);
            }

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
            try
            {
                using (MySqlCommand command = new MySqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "INSERT INTO " +
                        "clips " +
                        "(id)" +
                        " VALUES " +
                        "(@id)";
                    command.Parameters.AddWithValue("@id", clip.id);                    
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logging.Log(ex.Message);
                Environment.Exit(0);
            }
            

        }
        /// <summary>
        /// Get The number of archived clips, needed to check if first run
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetNumberOfArchivedClips()
        {
            int result = 0;            
            try
            {
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
            }
            catch (Exception ex)
            {
                Program.Logging.Log(ex.Message);
                Environment.Exit(0);
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
            try
            {                
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
            }
            catch (Exception ex)
            {
                Program.Logging.Log(ex.Message);
                Environment.Exit(0);
            }
            return clipExists;
        }
    }
}
