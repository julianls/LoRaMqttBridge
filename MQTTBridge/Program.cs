using Microsoft.Extensions.Configuration;
using System;

namespace MQTTBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            // Adding JSON file into IConfiguration.
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            MessageProcessor messageProcessor = new MessageProcessor(config, new MessageBroker(config));
            messageProcessor.Start();
            Console.ReadLine();
            messageProcessor.Stop();
        }
    }
}
