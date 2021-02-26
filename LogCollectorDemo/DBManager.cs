using System;
using System.IO;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace LogControllerDemo
{
    public class DatabaseConfig
    {
        public string Mysql { get; set; }
        public string Mongo { get; set; }
    }

    public class DBManager
    {
        static string mysql = null;
        static MongoClient mongo = null;

        public static bool UseMySql(Func<MySqlCommand, bool> func)
        {
            using (var connection = new MySqlConnection(mysql))
            using (var command = new MySqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                return func(command);
            }
        }

        /// <summary>
        /// Mysql.Data library doesn't support true async. Run using Task.FromResult.
        /// Don't use where frame is important.
        /// </summary>
        /// <param name="funcAsync"></param>
        /// <returns></returns>
        public static async Task<bool> UseMySqlAsync(Func<MySqlCommand, Task<bool>> funcAsync)
        {
            using (var connection = new MySqlConnection(mysql))
            using (var command = new MySqlCommand())
            {
                await connection.OpenAsync();
                command.Connection = connection;
                return await funcAsync(command);
            }
        }

        public static bool UseMongo(Func<MongoClient, bool> func)
        {
            return func(mongo);
        }

        public static async Task<bool> UseMongoAsync(Func<MongoClient, Task<bool>> funcAsync)
        {
            return await funcAsync(mongo);
        }

        public static void Initialize(string path)
        {
            if (Util.PathHelper.TryFindFilePathFromAppLocation(path, out var newPath) == false)
            {
                throw new FileNotFoundException($"{path} not found.");
            }

            var json = File.ReadAllText(newPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("config empty.");
            }

            var config = JsonConvert.DeserializeObject<DatabaseConfig>(json);

            if (string.IsNullOrWhiteSpace(config.Mysql))
            {
                throw new InvalidDataException("mysql connection string is empty.");
            }

            if (string.IsNullOrWhiteSpace(config.Mongo))
            {
                throw new InvalidDataException("mongo connection string is empty.");
            }

            string dbType = null;
            string connectionString = null;

            try
            {
                dbType = nameof(config.Mysql);
                connectionString = config.Mysql;

                using var context = new MySqlConnection(config.Mysql);
                context.Open();
                if (context.Ping() == false)
                {
                    throw new Exception("Ping fail.");
                }

                mysql = config.Mysql;

                dbType = nameof(config.Mongo);
                connectionString = config.Mongo;

                MongoClient client = new MongoClient(config.Mongo);
                client.GetDatabase("Test").RunCommand((Command<BsonDocument>)"{ ping: 1 }");

                mongo = client;
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection fail. DBType: {dbType}, Connection: {connectionString}", ex);
            }
        }
    }
}
