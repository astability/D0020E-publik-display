using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Web;

namespace HtmlLayout.Pages
{
    public class IndexModel : PageModel
    {
        public string[] slideshow;
        public string[] texts;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

        }

        public void OnGet()
        {
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
        }
    }
}
