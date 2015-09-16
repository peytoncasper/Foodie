using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace RaspPiHub.BluetoothManager
{
    public class BluetoothManager
    {
        public delegate void AddOnMessageReceivedDelegate(object sender, SensorReading sensorReading);
        public event AddOnMessageReceivedDelegate WeightReceived;
        private void OnWeightReceivedEvent(object sender, SensorReading sensorReading)
        {
            if (WeightReceived != null)
                WeightReceived(sender, sensorReading);
        }
        public enum BluetoothConnectionState
        {
            Disconnected,
            Connected,
            Connecting
        }
        
        public DeviceInformationCollection DeviceCollection { get; set; }
        public DeviceInformation SelectedDevice { get; set; }
        public RfcommDeviceService BluetoothService { get; set; }
        public DataReader Reader { get; set; }
        public Task BluetoothListener { get; set; }
        public DataWriter Writer { get; set; }
        public StreamSocket Socket { get; set; }
        public Guid SensorId { get; set; }
        public BluetoothConnectionState State { get; set; }

        private DispatcherTimer timer = new DispatcherTimer();

        public BluetoothManager()
        {

            timer.Tick += timer_tick;
            timer.Interval = TimeSpan.FromSeconds(12);
        }
        public async Task ScanForDevices()
        {
            try
            {
                DeviceCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));
            }
            catch (Exception exception)
            {
            }
        }

        public async Task ConnectToDevice(string deviceName, Guid sensorId)
        {


            //if (DeviceCollection == null)
                await ScanForDevices();

            foreach (var item in DeviceCollection)
            {
                if (item.Name == deviceName)
                {
                    SelectedDevice = item;
                    break;
                }
            }

            if (SelectedDevice != null)
            {

                BluetoothService = await RfcommDeviceService.FromIdAsync(SelectedDevice.Id);

                if (BluetoothService != null)
                {
                    try
                    {
                        if (Socket != null || Reader != null || BluetoothListener != null || Writer != null)
                            Disconnect();

                        Socket = new StreamSocket();

                        timer.Start();
                        await Socket.ConnectAsync(BluetoothService.ConnectionHostName, BluetoothService.ConnectionServiceName);
                        timer.Stop();
                        if (Socket != null)
                        {
                            Writer = new DataWriter(Socket.OutputStream);
                            Reader = new DataReader(Socket.InputStream);

                            State = BluetoothConnectionState.Connected;
                            SensorId = sensorId;
                        }
                        
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

        }
        public void Disconnect()
        {
            State = BluetoothConnectionState.Disconnected;
            if (Reader != null)
                Reader = null;
            if (Writer != null)
            {
                Writer.DetachStream();
                Writer = null;
            }
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }
            if (BluetoothService != null)
                BluetoothService = null;
            if(BluetoothListener != null)
                BluetoothListener = null;

        }
        private void timer_tick(object sender, object e)
        {
            timer.Stop();
            Disconnect();
        }
        public async Task<uint> SendMessageAsync(string message)
        {
            uint sentMessageSize = 0;
            if (Writer != null)
            {
                uint messageSize = Writer.MeasureString(message);
                Writer.WriteByte((byte)messageSize);
                sentMessageSize = Writer.WriteString(message);
                await Writer.StoreAsync();
            }
            return sentMessageSize;
        }
        public async Task ListenForMessagesAsync()
        {
            while (Reader != null)
            {
                try
                {

                    //// Read first byte (length of the subsequent message, 255 or less). 
                    uint sizeFieldCount = 0;
                    if (State == BluetoothConnectionState.Connected)
                        sizeFieldCount = await Reader.LoadAsync(1);
                    else
                        return;
                    if (sizeFieldCount != 1)
                    {
                        // The underlying socket was closed before we were able to read the whole data. 
                        return;
                    }

                    //// Read the message.
                    uint messageLength = 0;
                    if (State == BluetoothConnectionState.Connected)
                        messageLength = uint.Parse(((char)Reader.ReadByte()).ToString());
                    else
                        return;

                    uint actualMessageLength = 0;
                    if (State == BluetoothConnectionState.Connected)
                        actualMessageLength = await Reader.LoadAsync(messageLength);
                    else
                        return;
                    if (messageLength != actualMessageLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data. 
                        return;
                    }
                    // Read the message and process it.
                    string message = "";
                    if (State == BluetoothConnectionState.Connected)
                        message = Reader.ReadString(actualMessageLength);
                    else
                        return;

                    string payload = message.Split(':')[1];
                    if(message.Count() > 1)
                        switch(message[0])
                        {

                            case 'W':
                                OnWeightReceivedEvent(this, new SensorReading() { Reading_Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), Sensor_Id = SensorId, Weight = double.Parse(payload) });
                                return;

                        }

                }
                catch (Exception ex)
                {

                }
            }
        }


    }
}
