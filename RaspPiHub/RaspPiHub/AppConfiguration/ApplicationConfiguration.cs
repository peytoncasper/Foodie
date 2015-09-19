using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspPiHub.AppConfiguration
{
    public class ApplicationConfiguration
    {
        public string StorageConnectionString { get; set; }
        public string ServiceBusNamespace { get; set; }
        public string EventHubName { get; set; }
        public string PolicyName { get; set; }
        public string PolicyKey { get; set; }
        public string PublisherName { get; set; }
        public List<SensorProfile> ConnectedSensors { get; set; }

        public TimeSpan TokenTimeToLive = new TimeSpan(0, 20, 0);
        public string DefaultVerificationCode { get; set; }
        public string AzureVMIp { get; set; }
    }
}
