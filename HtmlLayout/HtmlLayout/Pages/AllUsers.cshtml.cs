using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PublikDisplay.Pages
{
    public class AllUsersModel : PageModel
    {
        public string[] userNameArray;
        public string[] roleArray;
        public int userNameCount;
        public void OnGet()
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", "{}");
            var result = collection.Find("{}").ToList();
            userNameCount = result.Count();
            userNameArray = new string[result.Count];
            roleArray = new string[result.Count];
            int nr = 0;
            foreach (var item in result)
            {
                userNameArray[nr] = item["username"].ToString();
                roleArray[nr] = item["role"].ToString();
                nr++;
            }
            

        }
      
    }
}
