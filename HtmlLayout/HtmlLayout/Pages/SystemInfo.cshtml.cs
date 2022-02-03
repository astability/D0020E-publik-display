using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PublikDisplay.Pages
{
    public class SystemInfoModel : PageModel
    {
        public string message;
        public void OnGet([FromRoute] int id)
        {
            message = id.ToString();
        }
    }
}
