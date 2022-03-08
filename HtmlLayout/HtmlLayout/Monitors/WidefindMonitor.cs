using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Widefind;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Timers;
using dbEnums;
using System.Collections.Generic;

namespace PublikDisplay.Monitors
{       
    public class WidefindMonitor : Imonitor
    {
        private int SystemId;
        private MongoClient Client;
        private iWidefindReader reader;

        private string humanName;
        private string collectionName;

        private IMongoCollection<BsonDocument> logsCollection;
        private IMongoCollection<BsonDocument> deviceCollection;
        private IMongoCollection<BsonDocument> systemCollection;

        private System.Timers.Timer timeoutCheckTimer;
        private System.Timers.Timer messageTimeoutTimer;
        private System.Timers.Timer retryTimer;

        public WidefindMonitor(int SystemId, MongoClient Client)
        {
            this.SystemId = SystemId;
            this.Client = Client;

            IMongoDatabase db = Client.GetDatabase("display");
            this.systemCollection = db.GetCollection<BsonDocument>("systems");

            var system = systemCollection.Find("{ \"systemId\": "+ SystemId +" }").FirstOrDefault();

            humanName = (string)system.GetValue("humanName");
            collectionName = (string)system.GetValue("collectionName");
            if ((string)system.GetValue("systemType") != "Widefind")
            {
                throw new Exception("Invalid system type.");
            }

            this.logsCollection = db.GetCollection<BsonDocument>(collectionName + "Logs");
            this.deviceCollection = db.GetCollection<BsonDocument>(collectionName + "Devices");

            SettingsData settings = FetchSettings();

            try
            {
                reader = new WidefindReader();
                reader.OnMessage += handleMessage;
                reader.OnError += handleError;
                reader.OnConnectionFailure += handleConnFail;

                this.timeoutCheckTimer = new System.Timers.Timer(settings.timeoutMs);
                timeoutCheckTimer.AutoReset = true;
                timeoutCheckTimer.Elapsed += checkTimeouts;

                this.messageTimeoutTimer = new System.Timers.Timer(settings.timeoutMs);
                messageTimeoutTimer.AutoReset = true;
                messageTimeoutTimer.Elapsed += handleMessageTimeout;
                

                reader.Connect(settings.mqttIp);

                // Code below will not execute if connection fails!
                // Make sure to duplicate code in retryConnection, to run it again after connect attempt.
                // Also stop timers in handleConnFail
                timeoutCheckTimer.Enabled = true;
                messageTimeoutTimer.Enabled = true;
                
            }
            catch (uPLibrary.Networking.M2Mqtt.Exceptions.MqttConnectionException)
            {
                string message = "Första koppling till MQTT-broker misslyckades. ";
                CheckCondition(logsCollection, true, "Kan ej koppla till MQTT-broker.", message, importance.Failure, null, StatusCode.FatalNoConn);
                retryTimer = new System.Timers.Timer(10000);
                retryTimer.Elapsed += retryConnection;
                retryTimer.AutoReset = false;
                retryTimer.Enabled = true;

            }
            
        }

        private void handleConnFail(object sender, EventArgs e)
        {
            // Situation:
            // Connection has been timed out for too long and needs to be restarted completely.
            // Disable all other timers and start running retry connection timer.
            timeoutCheckTimer.Stop();
            messageTimeoutTimer.Stop();

            string message = "Tidsgräns för anslutning till MQTT-broker överskriden. Koppling kommer periodiskt försöka startas om.";
            CheckCondition(logsCollection, true, "Koppling till MQTT-broker avbruten.", message, importance.Failure, null, StatusCode.FatalNoConn);
            retryTimer = new System.Timers.Timer(10000);
            retryTimer.Elapsed += retryConnection;
            retryTimer.AutoReset = false;
            retryTimer.Enabled = true;
        }

        private void checkTimeouts(object sender, ElapsedEventArgs e)
        {
            SettingsData settings = FetchSettings();
            List<BsonDocument> devices = deviceCollection.Find("{}").ToList();
            DateTime timeOutDate = DateTime.Now.AddMilliseconds(-settings.timeoutMs);

            foreach (BsonDocument device in devices)
            {
                switch (device["type"].ToString())
                {
                    case "Beacon":
                        string message = "Fyr " + device["deviceId"] + " har inte skickat ut data i en period mer än " + settings.timeoutMs + "ms.";
                        CheckCondition(logsCollection, device["lastUpdate"] <= timeOutDate, "Ingen data från fyr " + device["deviceId"], message, importance.Warning, device["deviceId"].ToString(), StatusCode.WarnBeaconLost);
                        break;
                    case "Tag":
                        message = "Tagg " + device["deviceId"] + " har inte skickat ut data i en period mer än " + settings.timeoutMs + "ms. Taggen har troligtvis flyttats ur området eller stängts av.";
                        CheckCondition(logsCollection, device["lastUpdate"] <= timeOutDate, "Ingen data från tagg " + device["deviceId"], message, importance.Info, device["deviceId"].ToString(), StatusCode.InfoLostTag);
                        break;
                    default:
                        throw new NotImplementedException("Missing implementation for device type " + device["type"]);
                        break;
                }
            }


        }

        // No messages sent in a while, something worth noting to the user.
        private void handleMessageTimeout(object sender, ElapsedEventArgs e)
        {
            SettingsData settings = FetchSettings();
            string message = "Inga MQTT-meddelanden har mottagits i en period mer än " + settings.timeoutMs + "ms. Koppling till systemet är förmodligen förlorad.";
            CheckCondition(logsCollection, true, "Inga meddelanden mottagna", message, importance.Failure, null, StatusCode.FatalNoMsgs);
        }

        private void retryConnection(object sender, ElapsedEventArgs e)
        {
            SettingsData settings = FetchSettings();
            try
            {
                reader.Connect(settings.mqttIp);
                // We got this far? Horray, we have actually managed to connect!
                EndCondition(logsCollection, null, StatusCode.FatalNoConn);
                timeoutCheckTimer.Enabled = true;
                messageTimeoutTimer.Enabled = true;
            }
            catch (uPLibrary.Networking.M2Mqtt.Exceptions.MqttConnectionException)
            {
                // Keep on trying
                retryTimer.Enabled = true;
            }
            

        }

        private void handleMessage(object sender, WidefindMsgEventArgs e)
        {
            WidefindMsg msg = e.Message!.Value;
            BsonDocument? listEntry = deviceCollection.Find("{ \"deviceId\": \"" + msg.deviceId + "\" }").FirstOrDefault();
            SettingsData settings = FetchSettings();

            messageTimeoutTimer.Interval = settings.timeoutMs; // reset timer
            EndCondition(logsCollection, null, StatusCode.FatalNoMsgs);

            LogEvent(logsCollection, "Mottaget mqtt-meddelande från " + msg.deviceId, e.OriginalData , importance.Verbose, msg.deviceId, ConditionStatus.OneTime, StatusCode.VerboseMsg);

            if (listEntry == null)
            {
                // Device is new.
                switch (msg.type)
                {
                    case WidefindDeviceType.Beacon:
                        LogEvent(logsCollection, "Ny fyrenhet hittad " + msg.deviceId, "", importance.Info, msg.deviceId, ConditionStatus.OneTime, StatusCode.InfoNewBeacon);
                        break;
                    case WidefindDeviceType.Tag:
                        LogEvent(logsCollection, "Ny tagg hittad " + msg.deviceId, "", importance.Info, msg.deviceId, ConditionStatus.OneTime, StatusCode.InfoNewTag);
                        break;
                    default:
                        throw new NotImplementedException("Missing implementation for device type " + msg.type.ToString());
                        break;
                }
            }

            // Status checks
            string feedbackMsg = "Signalstyrka för " + msg.deviceId + " är dålig med en RSSI av " + msg.rssi + "dB";
            CheckCondition(logsCollection, msg.rssi < settings.minRssi, "Dålig signalstyrka för " + msg.deviceId, feedbackMsg, importance.Warning, msg.deviceId, StatusCode.WarnBadSignal);
            
            feedbackMsg = "Driftspänning för " + msg.deviceId + " är låg med en spänning på " + msg.battery + "V";
            CheckCondition(logsCollection, msg.battery < settings.minVolt, "Låg driftspänning för " + msg.deviceId, feedbackMsg, importance.Warning, msg.deviceId, StatusCode.WarnLowBattery);


            // Update or add device to device list.

            // simple check to stop device from showing the wrong time.
            DateTime now = DateTime.Now;
            DateTime deviceTime = now < msg.time ? now : msg.time;

            BsonDocument newDevice = new BsonDocument();
            BsonDocument state = new BsonDocument();
            state.Add("rssi", msg.rssi);
            state.Add("batteryVoltage", msg.battery);
            state.Add("posX", msg.pos.X);
            state.Add("posY", msg.pos.Y);
            state.Add("posZ", msg.pos.Z);

            newDevice.Add("state", state);
            newDevice.Add("deviceId", msg.deviceId);
            newDevice.Add("type", msg.type.ToString());
            newDevice.Add("lastUpdate", deviceTime);

            // set device status
            // There should be some sane alternative to this, but i am not too sure.
            string query = "{ \"deviceId\":        \"" + msg.deviceId + "\","
                         + "  \"conditionStatus\": \"Ongoing\" }";

            List<BsonDocument> ongoingConds = logsCollection.Find(query).ToList();
            importance mostImportant = importance.Verbose;
            foreach(BsonDocument cond in ongoingConds) 
            {
                importance parsed = Enum.Parse<importance>(cond.GetValue("importance").AsString);
                if (parsed > mostImportant)
                {
                    mostImportant = parsed;
                }           
            }

            // very fugly, but it works
            deviceStatus status = deviceStatus.Normal;
            switch (mostImportant)
            {
                case importance.Verbose:
                case importance.Info:
                    status = deviceStatus.Normal;
                    break;
                case importance.Warning:
                    status = deviceStatus.Warning;
                    break;
                case importance.Failure:
                    status = deviceStatus.Failure;
                    break;
            }


            newDevice.Add("deviceStatus", status.ToString());

            if(listEntry == null)
            {
                deviceCollection.InsertOne(newDevice);
            }
            else
            {
                deviceCollection.ReplaceOne("{ \"deviceId\": \"" + msg.deviceId + "\" }", newDevice);
                
            }
        }

        private void handleError(object sender, WidefindMsgEventArgs e)
        {
            SettingsData settings = FetchSettings();
            messageTimeoutTimer.Interval = settings.timeoutMs; // reset timer
            EndCondition(logsCollection, null, StatusCode.FatalNoMsgs);

            LogEvent(logsCollection, "Mottaget meddelande med okänt format", e.OriginalData, importance.Info, null, ConditionStatus.OneTime, StatusCode.InfoUnkMsg);
        }


        // End ongoing condition
        private static void EndCondition(
            IMongoCollection<BsonDocument> logCollection,
            string? deviceId,
            StatusCode code)
        {
            string query = "{ " + (deviceId != null ? "\"deviceId\": \"" + deviceId + "\"," : "")
                         + "  \"statusCode\": \"" + code.ToString() + "\","
                         + "  \"conditionStatus\": \"Ongoing\" }";

            BsonDocument updateDef = new BsonDocument();
            BsonDocument set = new BsonDocument();
            set.Add("conditionStatus", "Ended");
            set.Add("dateEnded", DateTime.Now);
            updateDef.Add("$set", set);

            logCollection.UpdateOne(query, updateDef);
        }

        // TODO:  possibly refactor this GIANT method signature into something nicer.
        // TODO2: this method currently performs a database lookup for every check condition. Can this be improved?
        private static void CheckCondition(
            IMongoCollection<BsonDocument> logCollection,
            bool condition,
            string title,
            string description,
            importance importance,
            string? deviceId,
            StatusCode code)
        {

            string query = "{ " + (deviceId != null ? "\"deviceId\": \"" + deviceId + "\"," : "")
                         + "  \"statusCode\": \"" + code.ToString() + "\","
                         + "  \"conditionStatus\": \"Ongoing\" }";

            bool logItemExists = logCollection.Find(query).Limit(1).CountDocuments() > 0;

            if (condition && !logItemExists)
            {
                // The checked-against condition and there is no event detailing that it is happening.
                // Write it to the log.
                LogEvent(logCollection, title, description, importance, deviceId, ConditionStatus.Ongoing, code);
            }
            
            if (!condition && logItemExists)
            {
                // The checked-against condition is no longer occuring (we know it did since there is a log item detailing it)
                // Update the item to mark it as Ended.

                BsonDocument updateDef = new BsonDocument();
                BsonDocument set = new BsonDocument();
                set.Add("conditionStatus", "Ended");
                set.Add("dateEnded", DateTime.Now);
                updateDef.Add("$set", set);

                logCollection.UpdateOne(query, updateDef);
            }
        }

        private static void LogEvent(
            IMongoCollection<BsonDocument> logCollection,
            string title, 
            string description, 
            importance importance,
            string? deviceId,
            ConditionStatus msgStatus,
            StatusCode code)
        {
            BsonDocument logItem = new BsonDocument();
            logItem.Add("date", DateTime.Now);
            logItem.Add("title", title);
            logItem.Add("description", description);
            logItem.Add("importance", importance.ToString());
            logItem.Add("deviceId", (BsonValue)deviceId ?? (BsonValue)BsonNull.Value);
            logItem.Add("conditionStatus", msgStatus.ToString());
            logItem.Add("statusCode", code.ToString());
            logCollection.InsertOne(logItem);
        }

        private SettingsData FetchSettings()
        {
            BsonDocument root = systemCollection.Find("{ \"systemId\": " + SystemId + " }").FirstOrDefault();
            BsonDocument settings = root.GetElement("settings").ToBsonDocument();
            SettingsData data = new SettingsData();

            data.mqttIp = settings["Value"]["mqttIp"].AsString;
            data.timeoutMs = settings["Value"]["timeoutMs"].AsInt32;
            data.minRssi = settings["Value"]["minRssi"].AsDouble;
            data.minVolt = settings["Value"]["minVolt"].AsDouble;

            return data;
        }

        private enum StatusCode
        {
                            // Fatal errors:
           FatalNoConn,         //    Unable to start MQTT connection         
           FatalNoMsgs,         //    No mqtt messages for a large duration
           //FatalNoBeacons,    //    No beacons detected
    
                            // Warnings:
           WarnBeaconLost,      //    Beacon lost
           WarnLowBattery,      //    Low battery
           WarnBadSignal,       //    Bad signal
    
                            // Info:
           InfoNewBeacon,     //    Beacon added
           InfoNewTag,        //    Tag added
           InfoLostTag,       //    Tag lost
           InfoUnkMsg,        //    Unrecognized message 

                            // Verbose:
           VerboseMsg         //    Message recieved
        }

        private class SettingsData
        {
            public string mqttIp;
            public int timeoutMs;
            public double minRssi;
            public double minVolt;
        }

    }
}
