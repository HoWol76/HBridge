using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shutters
{
    interface IMqttSubscriber
    {
        Task Subscribe(string topic);
        Task Unsubscribe(string topic);

        event MessageEventHandler MessageReceived;

        event StatusEventHandler StatusChanged;
    }

    public class MessageEventArgs: EventArgs
    {
        public string ClientId { get; set; }
        public string Topic { get; set; }
        public string Payload { get; set; }
    }

    public delegate void MessageEventHandler(object sender, MessageEventArgs e);
}
