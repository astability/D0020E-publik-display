using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.IO;
using System.Text.Json;

namespace HtmlLayout
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Planned startup order:
            // 1) Database initialization from some JSON settings file
            //      -> Schema files?
            //      -> Initial system monitor settings?
            // 2) System monitors startup
            //      -> Params in constructor
            //      -> Params from JSON settings
            // 3) Start web hosting

            // Huge TODO: make this config file globally accessible somehow.            
            string CONFIG_PATH = "databaseConfig.json";
            string jsonText = System.IO.File.ReadAllText(CONFIG_PATH);
            ConfigData configData = JsonSerializer.Deserialize<ConfigData>(jsonText);

            MongoClient client = new MongoClient(configData.connectionString);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class ConfigData
    {
        public string connectionString { get; set; }
    }
}
