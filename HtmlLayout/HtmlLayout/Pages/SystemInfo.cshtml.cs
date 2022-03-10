using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PublikDisplay.Pages
{
    public class SystemInfoModel : PageModel
    {
        public string message;
        public string[] deviceSorting;
        public string[][] devices;
        public string[][] deviceState;
        public string[][] logs;
        public string[] systemInfo;
        public void OnGet([FromRoute] int? id)
        {
            message = id.ToString();
            var dbClient = new MongoClient("mongodb://127.0.0.1:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("systems");
            var filter = Builders<BsonDocument>.Filter.Eq("systemId", id);
            var system = collection.Find(filter).FirstOrDefault();
            if (system != null)
            {
                systemInfo = new string[]
                {
                    system.GetValue("humanName", null).AsString,
                    system.GetValue("systemStatus", null).AsString,
                };
                deviceSorting = system.GetValue("deviceSorting", null).AsBsonArray.Select(p => p.AsString).ToArray();
                collection = db.GetCollection<BsonDocument>(system.GetValue("collectionName", null).AsString + "Devices");
                var docs = collection.Find(new BsonDocument()).ToList();
                var count = collection.CountDocuments(new BsonDocument());
                devices = new string[count][];
                deviceState = new string[count][];
                for (int i = 0; i < count; i++)
                {
                    var tempList = new String[] {
                    docs[i].GetValue("deviceId", null).AsString,
                    docs[i].GetValue("type", null).AsString,
                    docs[i].GetValue("lastUpdate", null).ToUniversalTime().ToString(),
                    docs[i].GetValue("deviceStatus", null).AsString,
                    };
                    devices[i] = tempList;
                    // Försök lägga till batteri
                }
                collection = db.GetCollection<BsonDocument>(system.GetValue("collectionName", null).AsString + "Logs");
                docs = collection.Find(new BsonDocument()).ToList();
                count = collection.CountDocuments(new BsonDocument());
                logs = new string[count][];
                for (int i = 0; i < count; i++)
                {
                    var tempList = new String[7];
                    tempList[0] = docs[i].GetValue("importance", null).AsString;
                    tempList[1] = docs[i].GetValue("title", null).AsString;
                    try
                    {
                        tempList[2] = docs[i].GetValue("deviceId", null).AsString;
                    }
                    catch (Exception ex)
                    {
                        tempList[2] = "-";
                    }
                    tempList[3] = docs[i].GetValue("conditionStatus", null).AsString;
                    tempList[4] = docs[i].GetValue("statusCode", null).AsString;
                    tempList[5] = docs[i].GetValue("date", null).ToUniversalTime().ToString();
                    tempList[6] = docs[i].GetValue("description", null).AsString;
                    logs[i] = tempList;
                }

            }
        }
    }
}
