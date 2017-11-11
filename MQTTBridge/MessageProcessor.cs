using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTTBridge
{
    public class MessageProcessor
    {
        private bool isStarted;
        private MqttClient client;
        private IMessageBroker messageBroker;
        private IConfiguration config;

        public MessageProcessor(IConfiguration config, IMessageBroker messageBroker)
        {
            this.config = config;
            this.messageBroker = messageBroker;
        }


        public void Start()
        {
            for (int i = 0; i < 5 && !isStarted; i++)
            {
                isStarted = true;
                try
                {
                    Console.WriteLine("MessageProcessor Starting");
                    StartClient();
                    Console.WriteLine("MessageProcessor Started");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    isStarted = false;
                    Console.WriteLine("MessageProcessor Start Error");
                }
            }
        }

        public void Stop()
        {
            isStarted = false;
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
                client = null;
            }
        }

        private void StartClient()
        {
            string ttnUri = config["TtnSettings:ConnectionUri"];
            client = new MqttClient(ttnUri, 1883, false, null, null, MqttSslProtocols.None);
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            string ttnConnectionName = config["TtnSettings:ConnectionName"];
            string ttnUser = config["TtnSettings:UserName"];
            string ttnPass = config["TtnSettings:UserPass"];
            client.Connect(ttnConnectionName, ttnUser, ttnPass);
            client.Subscribe(new string[] { "+/devices/+/up" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // access data bytes throug e.Message
            await messageBroker.ProcessMessage(Encoding.Default.GetString(e.Message));
        }
    }
}
