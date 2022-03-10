using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PublikDisplay.Monitors;
using MongoDB.Driver;
using MongoDB.Bson;
using dbEnums;

namespace HtmlLayout
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //  1) Start MongoDB connection.
            //  2) Setup monitioring
            //  3) Start site host

            /* var client = new MongoClient("mongodb://localhost");
            var db = client.GetDatabase("display");
            var systemsList = db.GetCollection<BsonDocument>("systems").Find("{ }").ToList();
            
            List<Imonitor> monitors = new List<Imonitor>();
            foreach(var system in systemsList)
            {
                Imonitor newMonitor;
                int id = system["systemId"].AsInt32;
                switch (Enum.Parse(typeof(systemType), (string)system["systemType"]))
                {
                    case systemType.Widefind:
                        newMonitor = new WidefindMonitor(id, client);
                        break;
                    case systemType.Fibaro:
                        newMonitor = new FibaroMonitor(id, client);
                        break;
                    default:
                        throw new InvalidOperationException("Failed to find parse provided system type.");
                }
                monitors.Add(newMonitor);
            } */

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder builder =  Host.CreateDefaultBuilder(args);

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });


            return builder;
        }
            
    }
}
