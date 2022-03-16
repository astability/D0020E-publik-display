using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Microsoft.AspNetCore.Http;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace PublikDisplay.Pages
{
    public class SystemInfoModel : PageModel
    {
        public string message;
        public string[] deviceSorting;
        public string[][] devices;
        public string[][] deviceState;
        public string[][] logs;
        public string[] logIds;
        public string[] systemInfo;
        public void OnGet([FromRoute] int? id)
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }
            message = id.ToString();
            var dbClient = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("systems");
            var filter = Builders<BsonDocument>.Filter.Eq("systemId", id);
            var system = collection.Find(filter).FirstOrDefault();
            if (system == null)
            {
                filter = Builders<BsonDocument>.Filter.Eq("systemId", 0);
                system = collection.Find(filter).FirstOrDefault();
                Response.Redirect("/");
            }
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
            }
            collection = db.GetCollection<BsonDocument>(system.GetValue("collectionName", null).AsString + "Logs");
            docs = collection.Find(new BsonDocument()).ToList();
            count = collection.CountDocuments(new BsonDocument());
            logs = new string[count][];
            logIds = new string[count];
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
                var tempString = docs[i].GetValue("_id", null).ToString();
                Console.WriteLine(tempString);
                logIds[i] = tempString.TrimStart('/');
                Console.WriteLine(tempString);
                Console.WriteLine(logs[i]);
            }
        }

        public void OnPostSolve([FromRoute] int? id, string logId)
        {
            var dbClient = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("systems");
            var filter = Builders<BsonDocument>.Filter.Eq("systemId", id);
            var system = collection.Find(filter).FirstOrDefault();
            if (system != null)
            {
                collection = db.GetCollection<BsonDocument>(system.GetValue("collectionName", null).AsString + "Logs");
                filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(logId.Remove(logId.Length - 1, 1)));
                var document = collection.Find(filter).FirstOrDefault();
                if (document != null)
                {
                    var update = Builders<BsonDocument>.Update.Set("conditionStatus", "Ended");
                    var options = new UpdateOptions { IsUpsert = true };
                    collection.UpdateOne(filter, update, options);
                }
            }
            OnGet(id);
        }

        public void OnPostHide([FromRoute] int? id, string logId)
        {
            var dbClient = new MongoClient("mongodb://localhost:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("systems");
            var filter = Builders<BsonDocument>.Filter.Eq("systemId", id);
            var system = collection.Find(filter).FirstOrDefault();
            if (system != null)
            {
                collection = db.GetCollection<BsonDocument>(system.GetValue("collectionName", null).AsString + "Logs");
                filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(logId.Remove(logId.Length - 1, 1)));
                var document = collection.Find(filter).FirstOrDefault();
                if (document != null)
                {
                    var update = Builders<BsonDocument>.Update.Set("conditionStatus", "Hidden");
                    var options = new UpdateOptions { IsUpsert = true };
                    collection.UpdateOne(filter, update, options);
                }
            }
            OnGet(id);
        }
    }
}
