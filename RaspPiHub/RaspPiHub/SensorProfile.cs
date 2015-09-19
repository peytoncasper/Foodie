using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspPiHub
{
    public class SensorProfile
    {
        public Guid SensorId { get; set; }
        public string BluetoothId { get; set; }
        public string BluetoothName { get; set; }
    }
}
