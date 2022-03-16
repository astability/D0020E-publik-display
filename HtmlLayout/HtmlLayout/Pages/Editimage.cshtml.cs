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
    public class EditimageModel : PageModel
    {
        private IHostingEnvironment ihostingEnvironment;

        [BindProperty]
        public string Imagename { get; set; }
        public string OldImagename { get; set; }

        public string ImageFileName { get; set; }
        public string FileName { get; set; }

        [BindProperty, MaxLength(800)]
        public string MainText { get; set; }

        public EditimageModel(IHostingEnvironment ihostingEnvironment)
        {
            this.ihostingEnvironment = ihostingEnvironment;
        }
        public void OnGet()
        {
        }
        public void OnGetEdit(string name)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("slideshow");
            var filter = Builders<BsonDocument>.Filter.Eq("pictureName", name);
            var info = collection.Find(filter).FirstOrDefault();
            if (info != null)
            {
                Imagename = info.GetValue("pictureName").ToString();
                ImageFileName = info.GetValue("image").ToString();
                MainText = info.GetValue("text").ToString();
            }

        }
        public void OnPostEditImageFile(IFormFile photo)
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

            var imageInfo = new BsonDocument
                { {"Image", photo.FileName.ToString()}};


            collection.FindOneAndReplace(filter, imageInfo);
            TempData["Msg"] = "Användarnamnet är ändrat !";
            Response.Redirect("/Allimages");

        }

        public void OnGetDelete(string name)
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("slideshow");
            var filter = Builders<BsonDocument>.Filter.Eq("pictureName", name);
            var info = collection.Find(filter).FirstOrDefault();
            if (info != null)
            {
                collection.DeleteOne(filter);
                Response.Redirect("/Allimages");
            }



        }


    }
}
