using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;

namespace PublikDisplay.Pages
{
    public class AllimagesModel : PageModel
    {

        public string[] imageName;
        public string[] imageArray;
        public string[] imageText;
        public int imagesCount;
        public void OnGet()
        {
            var Connection = "mongodb://localhost:27017";
            var client = new MongoClient(Connection);
            var db = client.GetDatabase("display");
            var collection = db.GetCollection<BsonDocument>("slideshow");
            var result = collection.Find("{}").ToList();
            imagesCount = result.Count();
            imageName = new string[result.Count];
            imageArray = new string[result.Count];
            imageText = new string[result.Count];
            int nr = 0;
            foreach (var item in result)
            {
                imageName[nr] = item["pictureName"].ToString();
                imageArray[nr] = item["image"].ToString();
                imageText[nr] = item["text"].ToString();
                nr++;
            }
            

        }
    }
}
