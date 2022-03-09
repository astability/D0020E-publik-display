using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Web;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HtmlLayout.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        // Slideshow and session variables
        public string loggedIn;
        public string role;
        public string[] slideshow;
        public string[] texts;

        // Message for login
        public string loginMessage;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

        }

        public void OnGet()
        {
            loggedIn = HttpContext.Session.GetString("loggedIn");
            loginMessage = HttpContext.Session.GetString("loginMessage");
            if (loggedIn == null)
            {
                loggedIn = "false";
            }
            slideshow = new string[] { "ALabb1.jpg", "ALabb2.jpg", "ALabb3.jpg",
                "ALabb4.jpg", "ALabb5.jpg" };
            texts = new string[] { "ALabb1.txt", "ALabb2.txt", "ALabb3.txt", "ALabb4.txt", "ALabb5.txt" };
            for (var i = 0; i < texts.Length; i++)
            {
                var filePath = "/wwwroot/imageText/" + texts[i];
                filePath = System.IO.Directory.GetCurrentDirectory() + filePath;
                if (System.IO.File.Exists(filePath))
                {
                    texts[i] = System.IO.File.ReadAllText(filePath);
                }
                else
                {
                    texts[i] = "Filen " + texts[i] + " kunde inte hittas";
                }
            }
            if(loggedIn == "false")
            {
                if (!String.IsNullOrEmpty(Request.Query["username"].ToString()) )
                {
                    loginBtn();
                }
            }
        }

        private void loginSuccess()
        {
            HttpContext.Session.SetString("loggedIn", "true");
            HttpContext.Session.SetString("loginMessage", "Lyckad inloggning");
            loggedIn = HttpContext.Session.GetString("loggedIn");
            Response.Redirect(Request.Path);
        }

        private void loginBtn()
        {
            var dbClient = new MongoClient("mongodb://127.0.0.1:27017");
            IMongoDatabase db = dbClient.GetDatabase("display");

            var collection = db.GetCollection<BsonDocument>("users");
            var filter = Builders<BsonDocument>.Filter.Eq("username", Request.Query["username"].ToString());
            var user = collection.Find(filter).FirstOrDefault();
            if (user != null)
            {
                if (Request.Query["username"].ToString() == user.GetValue("username").ToString())
                {
                    if (Request.Query["password"].ToString() == user.GetValue("password").ToString())
                    {
                        HttpContext.Session.SetString("role", user.GetValue("role").ToString());
                        loginSuccess();
                    }
                    else
                    {
                        HttpContext.Session.SetString("loginMessage", "Fel användarnamn eller lösenord");
                    }
                }
                else
                {
                    HttpContext.Session.SetString("loginMessage", "Fel användarnamn eller lösenord");
                }
            }
            HttpContext.Session.SetString("loginMessage", "Fel användarnamn eller lösenord");
            Response.Redirect(Request.Path);
        }

        public void OnGetLogoutBtn(Object sender, EventArgs e)
        {
            HttpContext.Session.SetString("role", "Besökare");
            HttpContext.Session.SetString("loggedIn", "false");
            HttpContext.Session.SetString("loginMessage", "");
            loggedIn = HttpContext.Session.GetString("loggedIn");
            OnGet();
        }
    }
}
