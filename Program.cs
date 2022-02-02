using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;



using var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", "Basic dW5pY29ybkBsdHUuc2U6S3MybXBIMkFsIQ==");
var result = await client.GetAsync("http://130.240.114.44/api/devices/");
Console.WriteLine("HTTP Code: " + result.StatusCode);
string med = await result.Content.ReadAsStringAsync();

Console.WriteLine(med);

fibarocs a = new fibarocs();