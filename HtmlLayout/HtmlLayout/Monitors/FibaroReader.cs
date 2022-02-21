using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace Fibaro
{

    public abstract class iFibaroReader
    {
        public abstract FibaroDevice[] GetDevices();
    }


    public class FibaroReader : iFibaroReader
    {
        private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };
        private readonly string authString;
        private readonly string adress;

        public FibaroReader(string address, string username, string password)
        {
            this.adress = address;
            
            byte[] bytes = Encoding.UTF8.GetBytes(username + ":" + password);
            this.authString = "Basic " + System.Convert.ToBase64String(bytes);
        }

        public override FibaroDevice[] GetDevices()
        {

            // TODO: find out how refresh state logs work
            // http://130.240.114.44/api/refreshStates/

                
            WebClient client = new WebClient();
            client.Headers.Add("Authorization", authString);
            string resp = client.DownloadString(adress + "/api/devices/");

            Console.WriteLine(resp);

            FibaroDeviceJson[] devices = JsonSerializer.Deserialize<FibaroDeviceJson[]>(resp, JsonOptions)!;

            FibaroDevice[] returnval = new FibaroDevice[devices.Length];
            int listPos = 0;
            foreach (FibaroDeviceJson device in devices)
            {
                if (device.id is null || device.name is null || device.properties is null || device.enabled is null)
                {
                    throw new JsonException("Format check failed. Required property could not be parsed.");
                }


                FibaroPropertiesJson prop = device!.properties;
                FibaroDevice newdevice = new FibaroDevice(device.id!.Value, device.name!, device.enabled!.Value, prop.batteryLevel, prop.dead ?? false);
                returnval[listPos++] = newdevice;
            }

            return returnval;

        }

        // Fields are assgned at run-time by JSON deserializer.
        // Disable "fields are not assigned at run-time" warnings.
        private class FibaroDeviceJson
        {
#pragma warning disable 0649
            public int? id;
            public string? name;
            public bool? enabled;
            public FibaroPropertiesJson? properties;
#pragma warning restore 0649
        }

        private class FibaroPropertiesJson
        {
#pragma warning disable 0649
            public int? batteryLevel;
            public bool? batteryLowNotification;
            public bool? dead;
#pragma warning restore 0649
        }

    }


    /// <summary>
    /// Represents the state of a Fibaro device.
    /// </summary>
    /// <param name="id">Unique numeric ID for device.</param>
    /// <param name="name">Human-readable device name.</param>
    /// <param name="enabled">Set if device is enabled.</param>
    /// <param name="batteryLevel">Device battery level in percent. Null if device has no battery.</param>
    /// <param name="dead">
    ///   Checks if device is "dead" (i.e is not connected). Note that dead nodes can sometimes be reconnected without problem.
    /// </param>
    public readonly record struct FibaroDevice(
        int id,
        string name,
        bool enabled,
        int? batteryLevel,
        bool dead
    );

    

}
