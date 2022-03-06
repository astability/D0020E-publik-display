using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.Reflection;
using System.Numerics;
using System.Text.RegularExpressions;

using System.Globalization;

using System.IO;

using System.Timers;

namespace Widefind
{

    public abstract class iWidefindReader
    {
        public abstract void Connect(string ipaddr);
        public abstract void Disconnect();
        public abstract event EventHandler<WidefindMsgEventArgs>? OnMessage;
        public abstract event EventHandler<WidefindMsgEventArgs>? OnError;
        public abstract event EventHandler<EventArgs>? OnConnectionFailure;
    }

    public class WidefindReader : iWidefindReader
    {
        private MqttClient? client;
        private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };

        private void InvokeError(string data)
        {
            EventHandler<WidefindMsgEventArgs>? handler = OnError;
            WidefindMsgEventArgs eventArgs = new WidefindMsgEventArgs(Message:null, data);
            handler?.Invoke(this, eventArgs);
        }

        /// <summary> Connect to a widefind MQTT broker.</summary>
        /// <param name="ipaddr">IP address to connect to.</param>
        /// <exception cref="uPLibrary.Networking.M2Mqtt.Exceptions.MqttConnectionException>on failed connect.</exception>
        public override void Connect(string ipaddr)
        {
            client = new MqttClient(ipaddr);
            client.MqttMsgPublishReceived += handleMessage;
            client.ConnectionClosed += handleConnClosed;

            client.Subscribe(new string[] { "ltu-system/#" }, new byte[] { 2 });

            // Use a unique id as client id, each time we start the application.
            client.Connect(Guid.NewGuid().ToString());
        }

        private void handleConnClosed(object sender, EventArgs e)
        {
            EventHandler<EventArgs>? handler = OnConnectionFailure;
            EventArgs eventArgs = new EventArgs();
            handler?.Invoke(this, eventArgs);
        }

        private void handleMessage(object sender, MqttMsgPublishEventArgs e)
        {
            string MqttMessage = Encoding.UTF8.GetString(e.Message);

            // This exact message is produced twice when first connecting to the broker for some reason.
            if (MqttMessage == "test message") { return; }

            WidefindJsonMsg JSON;
            try
            {
                // Cannot find any documentation on when this actually returns null.
                JSON = JsonSerializer.Deserialize<WidefindJsonMsg>(MqttMessage, JsonOptions)!;
            }
            catch (JsonException)
            {
                // Failure condition: malformed message.
                InvokeError(MqttMessage);
                return;
            }


            // No field can be null.
            foreach (FieldInfo propInfo in JSON.GetType().GetFields())
            {
                object? val = propInfo.GetValue(JSON);
                if (val == null)
                {
                    // Failure condition: malformed message.
                    InvokeError(MqttMessage);
                    return;
                }

            }
            

            try
            {
                string msg = JSON.message!;

                // Parse message tag. Contents has this basic format
                // <type>:<csv>*<unknown>
                Regex structure = new Regex(@"^(?<type>[^:*]+):(?<csv>[^*]+)\*(?<unknown>.+)$");
                Match match = structure.Match(msg);
                if (!match.Success) 
                { 
                    InvokeError(MqttMessage);
                    return;
                }

                string type = match.Groups["type"].Value;
                string[] csv = match.Groups["csv"].Value.Split(',');
                // string unknown = match.Groups["unknown"].Value; // currently unused.

                CultureInfo dotDecimal = new CultureInfo("en")
                {
                    NumberFormat =
                    {
                        NumberDecimalSeparator = "."
                    }
                };

                WidefindDeviceType device;
                float battery;
                float rssi;
                Vector3 pos;
                switch (type)
                {
                    case "BEACON":
                        //csv fmt:
                        // <address>,<version>,<posX>,<posY>,<posZ>,<battery>,<rssi>,<timealive>,<calibration>,<nodetype>
                        device = WidefindDeviceType.Beacon;
                        pos = new Vector3(int.Parse(csv[2]), int.Parse(csv[3]), int.Parse(csv[4]));
                        battery = float.Parse(csv[5], dotDecimal);
                        rssi = float.Parse(csv[6], dotDecimal);
                        break;
                    case "REPORT":
                        //csv fmt:
                        // <address>,<version>,<posX>,<posY>,<posZ>,<velX>,<velY>,<velZ>,<battery>,<rssi>,<timealive>
                        device = WidefindDeviceType.Tag;
                        pos = new Vector3(int.Parse(csv[2]), int.Parse(csv[3]), int.Parse(csv[4]));
                        battery = float.Parse(csv[8], dotDecimal);
                        rssi = float.Parse(csv[9], dotDecimal);
                        break;
                    default:
                        // Failure condition: malformed message.
                        InvokeError(MqttMessage);
                        return;
                }
                
                WidefindMsg retval = new WidefindMsg(device, csv[0], pos, battery, rssi, JSON.time!.Value);

                EventHandler<WidefindMsgEventArgs>? handler = OnMessage;
                WidefindMsgEventArgs eventArgs = new WidefindMsgEventArgs(retval,MqttMessage);
                handler?.Invoke(this, eventArgs);
                return;

            }
            catch (FormatException)
            {
                // Failure condition: malformed message.
                InvokeError(MqttMessage);
                return;
            }
        }

        public override void Disconnect()
        {
            client?.Disconnect();
        }

        public override event EventHandler<WidefindMsgEventArgs>? OnMessage;
        public override event EventHandler<WidefindMsgEventArgs>? OnError;
        public override event EventHandler<EventArgs>? OnConnectionFailure;

        private class WidefindJsonMsg
        {
#pragma warning disable 0649
            public string? host;
            public string? message;
            public string? source;
            public DateTime? time;
            public string? type;
#pragma warning restore 0649
        };

    }

#if DEBUG
    public class FakeWidefindReader : iWidefindReader
    {
        private int pos;
        private List<string> fakeData = new List<string>();
        private string fakeDataPath = "Monitors/fakeWidefind.txt";
        private Timer messageTimer;
        private int startFails = 2;

        private readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };

        public override void Connect(string ipaddr)
        {
            if(startFails-- > 0)
            {
                throw new uPLibrary.Networking.M2Mqtt.Exceptions.MqttConnectionException("test exception",new Exception());
            }


            StreamReader sr = new StreamReader(fakeDataPath);
            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                fakeData.Add(line);
            }

            //*
            messageTimer = new Timer(1000);
            messageTimer.AutoReset = true;
            messageTimer.Elapsed += handleMessage;
            messageTimer.Start();
            //*/
        }

        private void InvokeError(string data)
        {
            EventHandler<WidefindMsgEventArgs>? handler = OnError;
            WidefindMsgEventArgs eventArgs = new WidefindMsgEventArgs(Message: null, data);
            handler?.Invoke(this, eventArgs);
        }

        private void handleMessage(object sender, ElapsedEventArgs e)
        {
            string MqttMessage = fakeData[pos];
            pos++;
            pos %= fakeData.Count;

            // This exact message is produced twice when first connecting to the broker for some reason.
            if (MqttMessage == "test message") { return; }

            WidefindJsonMsg JSON;
            try
            {
                // Cannot find any documentation on when this actually returns null.
                JSON = JsonSerializer.Deserialize<WidefindJsonMsg>(MqttMessage, JsonOptions)!;
            }
            catch (JsonException)
            {
                // Failure condition: malformed message.
                InvokeError(MqttMessage);
                return;
            }


            // No field can be null.
            foreach (FieldInfo propInfo in JSON.GetType().GetFields())
            {
                object? val = propInfo.GetValue(JSON);
                if (val == null)
                {
                    // Failure condition: malformed message.
                    InvokeError(MqttMessage);
                    return;
                }

            }


            try
            {
                string msg = JSON.message!;

                // Parse message tag. Contents has this basic format
                // <type>:<csv>*<unknown>
                Regex structure = new Regex(@"^(?<type>[^:*]+):(?<csv>[^*]+)\*(?<unknown>.+)$");
                Match match = structure.Match(msg);
                if (!match.Success)
                {
                    InvokeError(MqttMessage);
                    return;
                }

                string type = match.Groups["type"].Value;
                string[] csv = match.Groups["csv"].Value.Split(',');
                // string unknown = match.Groups["unknown"].Value; // currently unused.

                CultureInfo dotDecimal = new CultureInfo("en")
                {
                    NumberFormat =
                    {
                        NumberDecimalSeparator = "."
                    }
                };

                WidefindDeviceType device;
                float battery;
                float rssi;
                Vector3 pos;
                switch (type)
                {
                    case "BEACON":
                        //csv fmt:
                        // <address>,<version>,<posX>,<posY>,<posZ>,<battery>,<rssi>,<timealive>,<calibration>,<nodetype>
                        device = WidefindDeviceType.Beacon;
                        pos = new Vector3(int.Parse(csv[2]), int.Parse(csv[3]), int.Parse(csv[4]));
                        battery = float.Parse(csv[5], dotDecimal);
                        rssi = float.Parse(csv[6], dotDecimal);
                        break;
                    case "REPORT":
                        //csv fmt:
                        // <address>,<version>,<posX>,<posY>,<posZ>,<velX>,<velY>,<velZ>,<battery>,<rssi>,<timealive>
                        device = WidefindDeviceType.Tag;
                        pos = new Vector3(int.Parse(csv[2]), int.Parse(csv[3]), int.Parse(csv[4]));
                        battery = float.Parse(csv[8], dotDecimal);
                        rssi = float.Parse(csv[9], dotDecimal);
                        break;
                    default:
                        // Failure condition: malformed message.
                        InvokeError(MqttMessage);
                        return;
                }

                WidefindMsg retval = new WidefindMsg(device, csv[0], pos, battery, rssi, JSON.time!.Value);

                EventHandler<WidefindMsgEventArgs>? handler = OnMessage;
                WidefindMsgEventArgs eventArgs = new WidefindMsgEventArgs(retval, MqttMessage);
                handler?.Invoke(this, eventArgs);
                return;

            }
            catch (FormatException)
            {
                // Failure condition: malformed message.
                InvokeError(MqttMessage);
                return;
            }
        }

        public override void Disconnect()
        {
            
        }
        public override event EventHandler<WidefindMsgEventArgs>? OnMessage;
        public override event EventHandler<WidefindMsgEventArgs>? OnError;
        public override event EventHandler<EventArgs>? OnConnectionFailure;

        private class WidefindJsonMsg
        {
#pragma warning disable 0649
            public string? host;
            public string? message;
            public string? source;
            public DateTime? time;
            public string? type;
#pragma warning restore 0649
        };
    }
#endif

    public enum WidefindDeviceType
    {
        Tag,
        Beacon
    }

    /// <summary>
    /// Represents a state update message of a device belonging to a Widefind system.
    /// </summary>
    public readonly struct WidefindMsg
    {
        public WidefindMsg(WidefindDeviceType type, string deviceId, Vector3 pos, float battery, float rssi, DateTime time)
        {
            this.type = type;
            this.deviceId = deviceId;
            this.pos = pos;
            this.battery = battery;
            this.rssi = rssi;
            this.time = time;
        }

        public readonly WidefindDeviceType type;
        public readonly string deviceId;
        public readonly Vector3 pos;
        public readonly float battery;
        public readonly float rssi;
        public readonly DateTime time;
    }


    /// <summary>
    /// Represents argument data for a Widefind event triggered by a recieved MQTT message.
    /// </summary>
    /// <param name="Message">The parsed contents of the message. Null if message parsing failed.</param>
    /// <param name="OriginalData">Original recieved MQTT payload that triggered this event.</param>
    public class WidefindMsgEventArgs : EventArgs
    {
        public WidefindMsgEventArgs(WidefindMsg? Message, string OriginalData)
        {
            this.Message = Message;
            this.OriginalData = OriginalData;
        }
        public readonly WidefindMsg? Message;
        public readonly string OriginalData;
    }



    
    
}
