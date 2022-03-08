using Fibaro;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Timers;
using System.Text.Json;
using System.Text.Json.Serialization;
using dbEnums;
using System;
using System.Collections.Generic;
using System.Net;

namespace PublikDisplay.Monitors
{
    public class FibaroMonitor : Imonitor
    {
        private readonly int SystemId;
        private FibaroReader reader;

        private readonly MongoClient Client;
        private readonly IMongoDatabase db;
        private readonly IMongoCollection<BsonDocument> logsCollection;
        private readonly IMongoCollection<BsonDocument> deviceCollection;
        private readonly IMongoCollection<BsonDocument> systemCollection;

        private readonly Timer UpdateTimer;

        public FibaroMonitor(int SystemId, MongoClient Client)
        {

            this.SystemId = SystemId;
            this.Client = Client;

            db = Client.GetDatabase("display");
            this.systemCollection = db.GetCollection<BsonDocument>("systems");

            var system = systemCollection.Find("{ \"systemId\": " + SystemId + " }").FirstOrDefault();

            string humanName = (string)system.GetValue("humanName");
            string collectionName = (string)system.GetValue("collectionName");
            if ((string)system.GetValue("systemType") != "Fibaro")
            {
                throw new Exception("Invalid system type.");
            }

            this.logsCollection = db.GetCollection<BsonDocument>(collectionName + "Logs");
            this.deviceCollection = db.GetCollection<BsonDocument>(collectionName + "Devices");

            SettingsData settings = FetchSettings();
            this.reader = new FibaroReader(settings.serviceUrl, settings.serviceUser, settings.servicePassword);
            
            UpdateTimer = new Timer(settings.updatePeriodMs);
            UpdateTimer.AutoReset = true;
            UpdateTimer.Elapsed += readData;
            UpdateTimer.Enabled = true;
        }

        private void readData(object sender, ElapsedEventArgs e)
        {
            SettingsData settings = FetchSettings();

            try
            {
                FibaroDevice[] devices = reader.GetDevices();

                EndCondition(logsCollection, null, StatusCode.FatalConnTimeout);
                EndCondition(logsCollection, null, StatusCode.FatalConnNotAuth);
                EndCondition(logsCollection, null, StatusCode.FatalConnFailed);

                deviceCollection.DeleteMany("{ }");
                foreach (FibaroDevice device in devices)
                {
                    BsonDocument deviceDoc = new BsonDocument();
                    BsonDocument state = new BsonDocument();
                    state.Add("name", device.name);
                    state.Add("enabled", device.enabled);
                    state.Add("batteryLevel", (BsonValue?)device.batteryLevel ?? BsonNull.Value);
                    state.Add("dead", device.dead);

                    deviceDoc.Add("state", state);
                    deviceDoc.Add("deviceId", device.id.ToString());
                    deviceDoc.Add("type", "Enhet");
                    deviceDoc.Add("lastUpdate", DateTime.Now);

                    string message = "Enhet " + device.id + " (" + device.name + ") har en låg batterinivå på " + device.batteryLevel + "%.";
                    CheckCondition(logsCollection, (device.batteryLevel ?? 100) < settings.minBatteryLevel, "Batterinivå låg för enhet " + device.id, message, importance.Warning, device.id.ToString(), StatusCode.WarnLowBat);
                    message = "Enhet " + device.id + " (" + device.name + ") har markeras som död. Enhet kan potentiellt startas om från webbgränsnitt.";
                    CheckCondition(logsCollection, device.dead, "Enhet " + device.id + " 'död'", message, importance.Warning, device.id.ToString(), StatusCode.WarnDead);

                    // Fuglyness stolen from WidefindMonitor
                    string query = "{ \"deviceId\":        \"" + device.id + "\","
                                 + "  \"conditionStatus\": \"Ongoing\" }";

                    List<BsonDocument> ongoingConds = logsCollection.Find(query).ToList();
                    importance mostImportant = importance.Verbose;
                    foreach (BsonDocument cond in ongoingConds)
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


                    deviceDoc.Add("deviceStatus", status.ToString());
                    deviceCollection.InsertOne(deviceDoc);
                }
                
            }
            catch (System.Net.WebException ex)
            {
                // In case this error is caused because of invalid settings.
                this.reader = new FibaroReader(settings.serviceUrl, settings.serviceUser, settings.servicePassword);

                if (ex.Response == null)
                {
                    // Connection timeout
                    string message = "Kunde inte koppla REST-gränsnitt, tidsgräns för svar överskriden. ";
                    CheckCondition(logsCollection, true, "Server svarar inte. ", message, importance.Failure, null, StatusCode.FatalConnTimeout);
                    return;
                }

                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Invalid login
                    string message = "Konto saknas eller har ej korrekta detaljer för att kunna logga in i fibaro-hub. Logindetaljer kan ändras i inställningar.";
                    CheckCondition(logsCollection, true, "Logindetaljer ej accepterade. ", message, importance.Failure, null, StatusCode.FatalConnNotAuth);
                }
                else
                {
                    string message = "Koppling till server gjordes men misslyckades med ett HTTP-svar " + (int)response.StatusCode + " (" + response.StatusDescription + ").";
                    CheckCondition(logsCollection, true, "Koppling till server misslyckades.", message, importance.Failure, null, StatusCode.FatalConnFailed);
                }
                
                
            }
            catch (JsonException)
            {
                // TODO: implement.
                string message = "Mottag ett svar som inte kunde läsas som JSON.";
                LogEvent(logsCollection, "Felaktigt svar från server", message, importance.Info, null, ConditionStatus.OneTime, StatusCode.InfoJsonError);
            }


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
        // TODO3: this code is duplicated by other monitors. Maybe it should be moved to some inherited base class.
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

            data.updatePeriodMs = settings["Value"]["updatePeriodMs"].AsInt32;
            data.minBatteryLevel = settings["Value"]["minBatteryLevel"].AsInt32;
            data.serviceUrl = settings["Value"]["serviceUrl"].AsString;
            data.serviceUser = settings["Value"]["serviceUser"].AsString;
            data.servicePassword = settings["Value"]["servicePassword"].AsString;

            return data;
        }

        private class SettingsData
        {
            public int updatePeriodMs;
            public int minBatteryLevel;
            public string serviceUrl;
            public string serviceUser;
            public string servicePassword;  
        }

        private enum StatusCode
        {
            // Fatal
            FatalConnTimeout,
            FatalConnNotAuth,
            FatalConnFailed,
            
            // Warnings
            WarnLowBat,
            WarnDead,

            // Info
            InfoJsonError
        }

    }
}
