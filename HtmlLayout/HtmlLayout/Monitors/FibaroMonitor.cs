using Fibaro;
using MongoDB.Driver;

namespace PublikDisplay.Monitors
{
    public class FibaroMonitor
    {
        public FibaroMonitor(int SystemId, MongoClient Client)
        {
            FibaroReader fb = new FibaroReader("", "", "");
        }
    }
}
