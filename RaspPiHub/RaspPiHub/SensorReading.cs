using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspPiHub
{
    public class SensorReading
    {
        public Guid Sensor_Id { get; set; }
        public double Weight { get; set; }
        public string Reading_Time { get; set; }
    }
}
