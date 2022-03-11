using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System;

namespace PublikDisplay.Pages
{
    public class SystemSettings : PageModel
    {
        public string HumanName;
        public int systemId;
        public List<TableRow> tableRows = new List<TableRow>();

        public IActionResult OnGet([FromRoute] int? id)
        {

            if(id == null)
            {
                return Redirect("index");
            }

            systemId = id!.Value;

            MongoClient client = new MongoClient("mongodb://127.0.0.1");
            IMongoDatabase db = client.GetDatabase("display");

            IMongoCollection<BsonDocument> systemCollection = db.GetCollection<BsonDocument>("systems");
            BsonDocument systemData = systemCollection.Find("{ \"systemId\": "+ systemId + " }").FirstOrDefault();
            HumanName = systemData["humanName"].AsString;
            BsonDocument settings = systemData["settings"].AsBsonDocument;

            Dictionary<System.Type, string> TypeMap = new Dictionary<System.Type, string>();
            TypeMap.Add(typeof(BsonString), "text");
            TypeMap.Add(typeof(BsonInt32),  "number");
            TypeMap.Add(typeof(BsonInt64),  "number");
            TypeMap.Add(typeof(BsonDouble), "number");

            foreach (BsonElement field in settings)
            {
                System.Type FieldType = field.Value.GetType();
                TableRow row = new TableRow {
                    Name = field.Name,
                    InputType = TypeMap[FieldType],
                    Value = field.Value.ToString() };
                tableRows.Add(row);

            }

            return Page();
        }

        public IActionResult OnPostSubmit(Microsoft.AspNetCore.Http.IFormCollection input)
        {

            MongoClient client = new MongoClient("mongodb://127.0.0.1");
            IMongoDatabase db = client.GetDatabase("display");

            IMongoCollection<BsonDocument> systemCollection = db.GetCollection<BsonDocument>("systems");
            BsonDocument systemData = systemCollection.Find("{ \"systemId\": " + systemId + " }").FirstOrDefault();
            BsonDocument settings = systemData["settings"].AsBsonDocument;

            BsonDocument newSettings = new BsonDocument();
            Dictionary<BsonType, Func<string, BsonValue>> BsonMap = new Dictionary<BsonType, Func<string, BsonValue>>();
            BsonMap.Add(BsonType.String, a => (BsonString)a);
            BsonMap.Add(BsonType.Int32, a => (BsonInt32)Int32.Parse(a));
            BsonMap.Add(BsonType.Int64, a => (BsonInt64)Int64.Parse(a));
            BsonMap.Add(BsonType.Double, a => (BsonDouble)Double.Parse(a));
            foreach (var field in input)
            {
                // We do not care for CSRF protection.
                if (field.Key == "__RequestVerificationToken") { continue; }
                // None of this code can or should be able to handle arrays, so lets ignore them.
                if (field.Value.Count != 1) { continue; }
                BsonType type = settings[field.Key].BsonType;
                newSettings.Add(field.Key, BsonMap[type].Invoke(field.Value));
            }

            systemCollection.UpdateOne(Builders<BsonDocument>.Filter.Eq(x => x["systemId"], systemId),
                                       Builders<BsonDocument>.Update.Set("settings", newSettings));

            return this.OnGet(systemId);

        }
    }

    public struct TableRow
    {
        public string Name;
        public string InputType;
        public string Value;
    }
}
