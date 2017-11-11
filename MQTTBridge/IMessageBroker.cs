using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MQTTBridge
{
    public interface IMessageBroker
    {
        Task ProcessMessage(string rawMessage);
    }
}
