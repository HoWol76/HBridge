using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shutters
{
    interface IMqttPublisher
    {
        Task publish(string payload, string topic);
    }
}
