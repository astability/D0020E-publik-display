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

namespace HtmlLayout
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //  1) Start MongoDB connection.
            //  2) Setup monitioring
            //  3) Start site host




            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder builder =  Host.CreateDefaultBuilder(args);

            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

            /*
            builder.ConfigureServices(service =>
            {
                WidefindMonitor mon = new WidefindMonitor(1);
                service.AddSingleton<WidefindMonitor>(mon);
                WidefindMonitor mon2 = new WidefindMonitor(2);
                service.AddSingleton<WidefindMonitor>(mon2);
                
            });*/


            return builder;
        }
            
    }
}
