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
    public class CreateUserModel : PageModel
    {
        public class NewUser
        {

            Guid id { get; set; }

            [BindProperty]
            public string Username { get; set; }

            [BindProperty]
            public string Password { get; set; }

            [BindProperty]
            public string Role { get; set; }
        }
        public string Message { get; set; } = "Initial Request";
        

        public void OnGet()
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }
        }
        
        public async void OnPostSubmit(NewUser user)
        {
            if(user.Username.ToString().Length < 5)
            {

                TempData["Msg"] = "Användarnamn måste innehålla minst 5 bokstaver ";
            }
            else if(user.Password.ToString().Length < 6)
            {
                TempData["Msg"] = "Lösenordet måste innehålla minst 6 bokstaver eller siffror ";
            }
            else
            {
                var Connection = "mongodb://localhost:27017";
                var client = new MongoClient(Connection);
                var db = client.GetDatabase("display");
                var collection = db.GetCollection<BsonDocument>("users");
                var filter = Builders<BsonDocument>.Filter.Eq("username", user.Username.ToString());
                var info = collection.Find(filter).FirstOrDefault();
                
                if (info != null)
                {
                    if (user.Username.ToString() == info.GetValue("username").ToString())
                    {
                        TempData["Msg"] = "Användarnamnet är upptagen !";

                    }
                }
                else
                {
                    var userInfo = new BsonDocument
                    {
                        {"username", user.Username.ToString()},
                        {"password", user.Password.ToString()},
                        {"role", user.Role.ToString()},
                    };
                    collection.InsertOneAsync(userInfo);
                    TempData["Msg"] = "Ny användare är skapad";
                }
            }
        }
        /*
        public void OnPost()
        {
            Console.WriteLine(TxtUserName);
            var Connection = "mongondb://localhost";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("Display");
            var collection = db.GetCollection<BsonDocument>("users");
        }
        */
    }
}
