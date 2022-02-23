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
using LibUsbDotNet;
using LibUsbDotNet.Main;

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

        // USB setup
        //   Produkt-ID:	0x2200
        // Tillverkar - ID:	0x072f

        public static UsbDevice cardReader;
        public UsbDeviceFinder usbFinder = new UsbDeviceFinder(Convert.ToInt32("0x072F", 16), Convert.ToInt32("0x2200", 16));
        public string readValue;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;

        }

        public void OnGet()
        {
            loggedIn = HttpContext.Session.GetString("loggedIn");
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
        }

        public void OnGetLoginBtn(Object sender, EventArgs e)
        {
            HttpContext.Session.SetString("loggedIn", "true");
            loggedIn = HttpContext.Session.GetString("loggedIn");
            ErrorCode ec = ErrorCode.None;
            try
            {
                // Find and open the usb device.
                cardReader = UsbDevice.OpenUsbDevice(usbFinder);

                // If the device is open and ready
                if (cardReader == null) throw new Exception("Device Not Found.");

                // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                // it exposes an IUsbDevice interface. If not (WinUSB) the 
                // 'wholeUsbDevice' variable will be null indicating this is 
                // an interface of a device; it does not require or support 
                // configuration and interface selection.
                IUsbDevice wholeUsbDevice = cardReader as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    wholeUsbDevice.ClaimInterface(0);
                }

                // open read endpoint 1.
                UsbEndpointReader reader = cardReader.OpenEndpointReader(ReadEndpointID.Ep01);


                byte[] readBuffer = new byte[1024];
                while (ec == ErrorCode.None)
                {
                    int bytesRead;

                    // If the device hasn't sent data in the last 5 seconds,
                    // a timeout error (ec = IoTimedOut) will occur. 
                    ec = reader.Read(readBuffer, 5000, out bytesRead);

                    if (bytesRead == 0) throw new Exception(string.Format("{0}:No more bytes!", ec));
                    Console.WriteLine("{0} bytes read", bytesRead);
                    readValue = readValue + bytesRead.ToString();

                    // Write that output to the console.
                    Console.Write(Encoding.Default.GetString(readBuffer, 0, bytesRead));
                }

                Console.WriteLine("\r\nDone!\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
            }
            finally
            {
                if (cardReader != null)
                {
                    if (cardReader.IsOpen)
                    {
                        // If this is a "whole" usb device (libusb-win32, linux libusb-1.0)
                        // it exposes an IUsbDevice interface. If not (WinUSB) the 
                        // 'wholeUsbDevice' variable will be null indicating this is 
                        // an interface of a device; it does not require or support 
                        // configuration and interface selection.
                        IUsbDevice wholeUsbDevice = cardReader as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }

                        cardReader.Close();
                    }
                    cardReader = null;

                    // Free usb resources
                    UsbDevice.Exit();

                }
            }
            OnGet();
        }

        public void OnGetLogoutBtn(Object sender, EventArgs e)
        {
            HttpContext.Session.SetString("loggedIn", "false");
            loggedIn = HttpContext.Session.GetString("loggedIn");
            OnGet();
        }

    }
}
