using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace mqtttest
{
    internal class fibarocs
    {
        static async Task Main(string[] args)
        {
            using var client = new HttpClient();

            var result = await client.GetAsync("http://130.240.114.44/");
            Console.WriteLine(result.Content);
        }
    }
}
