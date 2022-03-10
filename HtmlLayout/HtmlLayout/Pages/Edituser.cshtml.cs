using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using Microsoft.AspNetCore.Http;

namespace PublikDisplay.Pages
{
    public class EditUserModel : PageModel
    {
        public class EditUser
        {

            Guid id { get; set; }

            [BindProperty]
            public string Username { get; set; }

            [BindProperty]
            public string OldUsername { get; set; }

            [BindProperty]
            public string Password { get; set; }

            [BindProperty]
            public string Role { get; set; }
        }
        public string Hname;
        public string userName;
        public string password;
        public string role;

        

        public void OnGet()
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }
        }
        public void OnGetEdit(string name)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", name);
            var info = collection.Find(filter).FirstOrDefault();
            if(info != null)
            {
                userName = info.GetValue("username").ToString();
                Hname = name;
                password = info.GetValue("password").ToString();
                role = info.GetValue("role").ToString();
            }
        }
        public void OnPostEditUserName(EditUser user)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", user.OldUsername.ToString());
            var info = collection.Find(filter).FirstOrDefault();
                    
           var userInfo = new BsonDocument
           {
               {"username", user.Username.ToString()},
               {"password", user.Password.ToString()},
               {"role", user.Role.ToString()},
            };

            collection.FindOneAndReplace(filter, userInfo);
            TempData["Msg"] = "Användarnamnet är ändrat !";
            Response.Redirect("/allusers");
        }
        public void OnPostEditPassword(EditUser user)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", user.OldUsername.ToString());
            var info = collection.Find(filter).FirstOrDefault();

            var userInfo = new BsonDocument
           {
               {"username", user.Username.ToString()},
               {"password", user.Password.ToString()},
               {"role", user.Role.ToString()},
            };

            collection.FindOneAndReplace(filter, userInfo);
            TempData["Msg"] = "Lösenordet är ändrat !";
            Response.Redirect("/allusers");
        }

        public void OnPostEditRole(EditUser user)
        {

            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", user.OldUsername.ToString());
            var info = collection.Find(filter).FirstOrDefault();

            var userInfo = new BsonDocument
           {
               {"username", user.Username.ToString()},
               {"password", user.Password.ToString()},
               {"role", user.Role.ToString()},
            };

            collection.FindOneAndReplace(filter, userInfo);
            TempData["Msg"] = "Användarnamnet är ändrat !";
            Response.Redirect("/allusers");
        }
        public void OnGetDelete(string name)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", name);
            var info = collection.Find(filter).FirstOrDefault();
            if (info != null)
            {
                collection.DeleteOne(filter);
                Response.Redirect("/allusers");
            }

        }


    }
}
