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
                    docs[i].GetValue("lastUpdate", null).AsString,
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
                    var tempList = new String[] {
                    docs[i].GetValue("importance", null).AsString,
                    docs[i].GetValue("title", null).AsString,
                    docs[i].GetValue("deviceId", null).AsString,
                    docs[i].GetValue("conditionStatus", null).AsString,
                    docs[i].GetValue("statusCode", null).AsString,
                    docs[i].GetValue("date", null).ToUniversalTime().ToString(),
                    docs[i].GetValue("description", null).AsString,
                    };
                    logs[i] = tempList;
                }

            }
        }
    }
}
