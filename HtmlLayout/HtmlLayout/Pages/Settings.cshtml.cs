using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PublikDisplay.Pages
{
    public class SettingsModel : PageModel
    {
        public void OnGet()
        {
            if (HttpContext.Session.GetString("loggedIn") == "false")
            {
                Response.Redirect("/");
            }
        }
    }
}
