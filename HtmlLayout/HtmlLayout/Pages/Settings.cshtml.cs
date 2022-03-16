using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;

namespace PublikDisplay.Pages
{ 
    public class SettingsModel : PageModel
    {
        public List<ListedSystem> SystemsList = new List<ListedSystem>();
        public void OnGet()
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }

            IMongoClient client = new MongoClient("mongodb://localhost");
            IMongoDatabase db = client.GetDatabase("display");
            IMongoCollection<BsonDocument> systemCollection = db.GetCollection<BsonDocument>("systems");
            List<BsonDocument> systems = systemCollection.Find("{}").ToList();
            foreach(var system in systems)
			{
                SystemsList.Add(new ListedSystem
                {
                    Name = system["humanName"].AsString,
                    Id = system["systemId"].AsInt32
                });
			}

        }
    }

    public struct ListedSystem
	{
        public string Name;
        public int Id;
	}
}
