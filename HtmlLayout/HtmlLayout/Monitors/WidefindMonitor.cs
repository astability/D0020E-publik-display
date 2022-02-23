using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Widefind;
using MongoDB.Driver;
using System;

namespace PublikDisplay.Monitors
{
    public class WidefindMonitor : IHostedService
    {
        private int Id;
        //private MongoClient Client;
        private WidefindReader? reader = null;

        public WidefindMonitor(int Id)
        {
            //discard useless abstract class that has no reason to exist whatsoever (?)
            this.Id = Id;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            reader = new WidefindReader();

            reader.OnMessage += handleMessage;
            //reader.OnError
            reader.Connect("130.240.74.55"); // change from hardcoded vals later.
            return;
        }

        private void handleMessage(object sender, WidefindMsgEventArgs e)
        {
            WidefindMsg msg = e.Message!.Value;
            Console.WriteLine("Got! " + msg.deviceID);
            
        }

        private void handleError(object sender, WidefindMsgEventArgs e)
        {

        }


        public async Task StopAsync(CancellationToken ct)
        {
            reader.Disconnect();
            return;
        }
    }
}
