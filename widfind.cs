using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

namespace mqtttest
{
    internal class widfind
    {
        MqttClient mqttClient;
        void Connect(string ipnumber)
        {
            mqttClient = new MqttClient(ipnumber);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            // Is this topic correct 
            mqttClient.Subscribe(new string[] { "#" }, new byte[] { 0 });

            // use a unique id as client id, each time we start the application
            string clientId = Guid.NewGuid().ToString();

            mqttClient.Connect(clientId);
        }

        void disconnect()
        {
            mqttClient.Disconnect();
        }
       


        void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);

            Console.WriteLine(ReceivedMessage);
        }
    }
}
