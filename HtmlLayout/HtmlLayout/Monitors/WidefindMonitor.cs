using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Widefind;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Timers;
using dbEnums;

namespace PublikDisplay.Monitors
{       
    public class WidefindMonitor
    {
        private int SystemId;
        private MongoClient Client;
        private WidefindReader reader;

        private string humanName;
        private string collectionName;

        private IMongoCollection<BsonDocument> logsCollection;
        private IMongoCollection<BsonDocument> deviceCollection;
        private IMongoCollection<BsonDocument> systemCollection;

        private System.Timers.Timer timeoutTimer;

        public WidefindMonitor(int SystemId, MongoClient Client)
        {
            this.SystemId = SystemId;
            this.Client = Client;

            IMongoDatabase db = Client.GetDatabase("display");
            this.systemCollection = db.GetCollection<BsonDocument>("systems");

            var system = systemCollection.Find<BsonDocument>("{ \"systemId\": "+ SystemId +" }").FirstOrDefault();

            humanName = (string)system.GetValue("humanName");
            collectionName = (string)system.GetValue("collectionName");
            if ((string)system.GetValue("systemType") != "Widefind")
            {
                throw new Exception("Invalid system type.");
            }

            this.logsCollection = db.GetCollection<BsonDocument>(collectionName + "Logs");
            this.deviceCollection = db.GetCollection<BsonDocument>(collectionName + "Devices");


            reader = new WidefindReader();
            reader.OnMessage += handleMessage;
            reader.OnError += handleError;

            //TODO: move timeout to var.
            this.timeoutTimer = new System.Timers.Timer(30_000); // 30s
            timeoutTimer.AutoReset = true;
            timeoutTimer.Elapsed += checkTimeouts;

            reader.Connect("130.240.74.55");
            timeoutTimer.Enabled = true;
        }

        private void checkTimeouts(object sender, ElapsedEventArgs e)
        {
            
        }

        private void handleMessage(object sender, WidefindMsgEventArgs e)
        {
            WidefindMsg msg = e.Message!.Value;
            BsonDocument? listEntry = deviceCollection.Find<BsonDocument>("{ \"deviceId\": \"" + msg.deviceID + "\" }").FirstOrDefault();

            //TODO: verbose logging


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

            newDevice.Add("deviceId", msg.deviceID);
            newDevice.Add("type", msg.type.ToString());
            newDevice.Add("lastUpdate", deviceTime.ToString());
            newDevice.Add("state", state);
            newDevice.Add("statusMessage", "Ok");
            newDevice.Add("deviceStatus", deviceStatus.Normal.ToString());

            if (listEntry == null)
            {
                // Device is new.

                deviceCollection.InsertOne(newDevice);
                // TODO: Log finding new device.

            }
            else
            {
                // Device still exists.
                // TODO: Update old logs
            }



            // ny device
            // borttappad device
            // updaterad device


            //msg.battery 

        }

        private void handleError(object sender, WidefindMsgEventArgs e)
        {

        }

        private static void LogEvent(
            IMongoCollection<BsonDocument> collection,
            string title, 
            string description, 
            string importance,
            string deviceId,
            ConditionStatus msgStatus,
            StatusCode code)
        {
            BsonDocument logItem = new BsonDocument();
            logItem.Add("title", title);
            logItem.Add("description", description);
            logItem.Add("importance", importance);
            logItem.Add("deviceId", deviceId);
            logItem.Add("conditionStatus", msgStatus.ToString());
            logItem.Add("statusCode", code.ToString());
            collection.InsertOne(logItem);
        }

        private BsonDocument FetchSettings()
        {
            BsonDocument root = systemCollection.Find("{ \"systemId\": " + SystemId + " }").FirstOrDefault();
            return root.GetElement("settings").ToBsonDocument();
        }

        private enum StatusCode
        {
                            // Fatal errors:
           FatalNoConn,         //    Unable to start MQTT connection         
           FatalNoMsgs,         //    No mqtt messages for a large duration
           FatalNoBeacons,      //    No beacons detected
    
                            // Warnings:
           WarnBeaconLost,      //    Beacon lost
           WarnLowBattery,      //    Low battery
           WarnBadSignal,       //    Bad signal
    
                            // Info:
           InfoNewBeacon,     //    Beacon added
           InfoNewTag,        //    Tag added
           InfoLostTag,       //    Tag lost
           InfoUnkMsg         //    Unrecognized message 
        }
    }
}
