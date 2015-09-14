using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace RaspPiHub.BluetoothManager
{
    public class BluetoothManager
    {
        public delegate void AddOnMessageReceivedDelegate(object sender, double weight);
        public event AddOnMessageReceivedDelegate WeightReceived;
        private void OnWeightReceivedEvent(object sender, double weight)
        {
            if (WeightReceived != null)
                WeightReceived(sender, weight);
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

        public BluetoothConnectionState State { get; set; }

        public bool Verified { get; set; }

        private string VerificationCode { get; set; }
        private string DefaultVerificationCode { get; set; }
       
        public BluetoothManager()
        {

            DefaultVerificationCode = App.Current.ApplicationConfiguration.DefaultVerificationCode;
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

        public async Task ConnectToDevice(string deviceName)
        {
            if(DeviceCollection == null)
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
                        await Socket.ConnectAsync(BluetoothService.ConnectionHostName, BluetoothService.ConnectionServiceName);


                        Writer = new DataWriter(Socket.OutputStream);
                        Reader = new DataReader(Socket.InputStream);
                        BluetoothListener = ListenForMessagesAsync();
                        State = BluetoothConnectionState.Connected;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

        }
        public void Disconnect()
        {
            
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

            State = BluetoothConnectionState.Disconnected;
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
        private async Task ListenForMessagesAsync()
        {
            while (Reader != null)
            {
                try
                {

                    //// Read first byte (length of the subsequent message, 255 or less). 
                    uint sizeFieldCount = await Reader.LoadAsync(1);
                    if (sizeFieldCount != 1)
                    {
                        // The underlying socket was closed before we were able to read the whole data. 
                        return;
                    }

                    //// Read the message. 
                    uint messageLength = uint.Parse(((char)Reader.ReadByte()).ToString());
                    uint actualMessageLength = await Reader.LoadAsync(messageLength);
                    if (messageLength != actualMessageLength)
                    {
                        // The underlying socket was closed before we were able to read the whole data. 
                        return;
                    }
                    // Read the message and process it.
                    string message = Reader.ReadString(actualMessageLength);
                    string payload = message.Split(':')[1];
                    if(message.Count() > 1)
                        switch(message[0])
                        {
                            case 'V':
                                Verify(payload);
                                break;
                            case 'W':
                                OnWeightReceivedEvent(this, double.Parse(payload));
                                break;
                        }

                }
                catch (Exception ex)
                {

                }
            }
        }
        private void Verify(string message)
        {
            if (message == VerificationCode || message == DefaultVerificationCode)
            {
                Verified = true;
                SendMessageAsync("V:" + Guid.NewGuid().ToString());
            }
            else
            {
                Disconnect();
                Verified = false;
            }
        }

    }
}
