﻿using System;
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

namespace Widefind
{

    public abstract class iWidefindReader
    {
        public abstract void Connect(string ipaddr);
        public abstract void Disconnect();
        public abstract event EventHandler<WidefindMsgEventArgs>? OnMessage;
        public abstract event EventHandler<WidefindMsgEventArgs>? OnError;
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

            client.Subscribe(new string[] { "ltu-system/#" }, new byte[] { 2 });

            // Use a unique id as client id, each time we start the application.
            int a = client.Connect(Guid.NewGuid().ToString());
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
                        device = WidefindDeviceType.beacon;
                        pos = new Vector3(int.Parse(csv[2]), int.Parse(csv[3]), int.Parse(csv[4]));
                        battery = float.Parse(csv[5], dotDecimal);
                        rssi = float.Parse(csv[6], dotDecimal);
                        break;
                    case "REPORT":
                        //csv fmt:
                        // <address>,<version>,<posX>,<posY>,<posZ>,<velX>,<velY>,<velZ>,<battery>,<rssi>,<timealive>
                        device = WidefindDeviceType.tag;
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

    public enum WidefindDeviceType
    {
        tag,
        beacon
    }

    /// <summary>
    /// Represents a state update message of a device belonging to a Widefind system.
    /// </summary>
    public readonly record struct WidefindMsg
    (
        WidefindDeviceType type,
        string deviceID,
        Vector3 pos,
        float battery,
        float rssi,
        DateTime time
    );

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
