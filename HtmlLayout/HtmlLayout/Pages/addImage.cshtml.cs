using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace PublikDisplay.Pages
{
    public class AddImageModel : PageModel
    {
        private IHostingEnvironment ihostingEnvironment;

        [BindProperty]
        public string Imagename { get; set; }
        public string FileName { get; set; }

        [BindProperty, MaxLength(300)]
        public string MainText { get; set; }

        public AddImageModel(IHostingEnvironment ihostingEnvironment)
        {
            this.ihostingEnvironment = ihostingEnvironment;
        }
        public void OnGet()
        {
        }
        public void OnPost(IFormFile photo )
        {
            var path = System.IO.Directory.GetCurrentDirectory() + "/wwwroot/images/" + photo.FileName;
            var stream = new FileStream(path, FileMode.Create);
            photo.CopyToAsync(stream);
            FileName = photo.FileName;
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("slideshow");
            var filter = Builders<BsonDocument>.Filter.Eq("pictureName", Imagename);
            var info = collection.Find(filter).FirstOrDefault();

            if (info != null)
            {
                if (Imagename == info.GetValue("pictureName").ToString())
                {
                    TempData["Msg"] = "Bildnamnet finns redan";

                }
            }
            else
            {
                var imageInfo = new BsonDocument
                    {
                        {"pictureName", Imagename},
                        {"image", photo.FileName.ToString()},
                        {"text", MainText},

                    };
                collection.InsertOneAsync(imageInfo);
                TempData["Msg"] = "Bilden är sparad";

            }

        }

    }
}
