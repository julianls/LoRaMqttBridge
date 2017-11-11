using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MQTTBridge
{
    public class MessageBroker : IMessageBroker
    {

        private string iotHubUri;
        private string registryConnectionString;

        private Dictionary<string, DeviceClient> deviceClients;
        private Microsoft.Azure.Devices.RegistryManager registryManager;
        private IConfiguration config;

        public MessageBroker(IConfiguration config)
        {
            this.config = config;
            deviceClients = new Dictionary<string, DeviceClient>();
            iotHubUri = config["AzureSettings:IotHubUri"];
            registryConnectionString = config["AzureSettings:RegistryConnectionString"];
            registryManager = Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(registryConnectionString);
        }

        public async Task ProcessMessage(string rawMessage)
        {
            try
            {
                dynamic messageObject = JsonConvert.DeserializeObject(rawMessage);

                string deviceId = messageObject.dev_id;
                DeviceClient deviceClient = await GetDeviceClient(deviceId);
                if (deviceClient != null)
                {
                    string payload = messageObject.payload_raw;
                    string appId = messageObject.app_id;
                    string jsonMessage = GetJSONMessage(payload, deviceId, appId);
                    var azureMessage = new Message(Encoding.ASCII.GetBytes(jsonMessage));
                    await deviceClient.SendEventAsync(azureMessage);
                    Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, jsonMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} > Error sending message: {1}", DateTime.UtcNow, ex);
            }
        }

        private string GetJSONMessage(string payload, string deviceId, string appId)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(payload);
            string message = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            //string message = payload;
            if(!message.StartsWith("{"))
                message = $"{{ rawData:\"{ message}\" }}";
            string jsonMessage = InjectDeviceIdAndTime(message, deviceId, appId, DateTime.UtcNow);
            return jsonMessage;
        }

        private string InjectDeviceIdAndTime(string message, string deviceId, string appId, DateTimeOffset timeOffset)
        {
            int jsonEnd = message.LastIndexOf('}');
            message = message.Insert(jsonEnd, string.Format(", \"ConnectionDeviceId\" : \"{0}\", \"AppId\" : \"{1}\", \"EnqueuedTime\" : \"{2}\" ", 
                deviceId, appId, timeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ")));
            return message;
        }

        private async Task<DeviceClient> GetDeviceClient(string deviceId)
        {
            DeviceClient deviceClient;
            if (!deviceClients.TryGetValue(deviceId, out deviceClient))
            {
                Microsoft.Azure.Devices.Device azureDevice = await registryManager.GetDeviceAsync(deviceId);

                if (azureDevice != null)
                {
                    deviceClient = DeviceClient.Create(
                        iotHubUri, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(
                            deviceId, azureDevice.Authentication.SymmetricKey.PrimaryKey), TransportType.Amqp);
                    deviceClients[deviceId] = deviceClient;
                }
            }

            return deviceClient;
        }
    }
}