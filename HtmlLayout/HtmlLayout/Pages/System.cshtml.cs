using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HtmlLayout.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<PrivacyModel> _logger;
        public string[] message;
        public string[][] systems;

        public PrivacyModel(ILogger<PrivacyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var dbClient = new MongoClient("mongodb://127.0.0.1:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("systems");
            var docs = collection.Find(new BsonDocument()).ToList();
            var count = collection.CountDocuments(new BsonDocument());
            systems = new string[count][];
            for (int i = 0; i < count; i++)
            {
                message = new String[] {
                docs[i].GetValue("systemId", null).ToString(),
                docs[i].GetValue("humanName", null).AsString,
                docs[i].GetValue("systemStatus", null).AsString,
                };
                systems[i] = message;
            }
        }
    }
}
