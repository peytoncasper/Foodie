using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Microsoft.IoT.Connections.Azure.EventHubs;
using System.Threading;
using Newtonsoft.Json;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RaspPiHub
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Storage Connection Objects
        /// </summary>
        private CloudStorageAccount storageAccount;
        private CloudQueueClient queueClient;
        private CloudQueue queue;
        /// 
        private ConnectionParameters connectionParams;
        private HttpSender eventHubSender;
        private DispatcherTimer timer = new DispatcherTimer();
        private BluetoothManager.BluetoothManager BluetoothManager; 

        //public string deviceName = "HC-06"; // Specify the device name to be selected; You can find the device name from the webb under bluetooth 

        public MainPage()
        {
            this.InitializeComponent();
            if((App.Current).ApplicationConfiguration == null)
            {
                throw new NullReferenceException("Not able to load Application Configuration");
            }
            InitializeEventHubSettings();


            BluetoothManager = new BluetoothManager.BluetoothManager();
            BluetoothManager.WeightReceived += UpdateWeight;




            timer.Interval = TimeSpan.FromSeconds(App.Current.ApplicationConfiguration.ConnectedSensors.Count * 5);
            timer.Tick += CheckWeight;
            timer.Start();
    
        }

        private async void CheckWeight(object sender, object e)
        {
            timer.Stop();

            foreach (var sensor in App.Current.ApplicationConfiguration.ConnectedSensors)
            {

                await BluetoothManager.ConnectToDevice(sensor.Value, sensor.Key);
                if (BluetoothManager.State == RaspPiHub.BluetoothManager.BluetoothManager.BluetoothConnectionState.Connected)
                {
                    
                    await BluetoothManager.SendMessageAsync("W");
                    await BluetoothManager.ListenForMessagesAsync();
                }
                BluetoothManager.Disconnect();
            }
            timer.Start();
            //if (BluetoothManager.State == RaspPiHub.BluetoothManager.BluetoothManager.BluetoothConnectionState.Connected)
            //{
            //    await BluetoothManager.SendMessageAsync("C:Verify");
            //}
        }
        private void SendWeightToEventHub(SensorReading sensorReading)
        {
            
            eventHubSender.Send(JsonConvert.SerializeObject(sensorReading));
        }
        private void InitializeEventHubSettings()
        {
            connectionParams = new ConnectionParameters(
                App.Current.ApplicationConfiguration.ServiceBusNamespace,
                App.Current.ApplicationConfiguration.EventHubName,
                App.Current.ApplicationConfiguration.PolicyName,
                App.Current.ApplicationConfiguration.PolicyKey,
                App.Current.ApplicationConfiguration.PublisherName,
                App.Current.ApplicationConfiguration.TokenTimeToLive);
            eventHubSender = new HttpSender(connectionParams);

        }



        private void UpdateWeight(object sender, SensorReading sensorReading)
        {
            SendWeightToEventHub(sensorReading);
            textBlock.Text = sensorReading.Weight.ToString();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {


            
        }

        private async void verify_Click(object sender, RoutedEventArgs e)
        {

        }

        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            BluetoothManager.Disconnect();
        }
    }
}
