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

        private BluetoothManager.BluetoothManager BluetoothManager; 

        public string deviceName = "HC-06"; // Specify the device name to be selected; You can find the device name from the webb under bluetooth 

        public MainPage()
        {
            this.InitializeComponent();
            if((App.Current).ApplicationConfiguration == null)
            {
                throw new NullReferenceException("Not able to load Application Configuration");
            }
            InitializeEventHubSettings();
            //InitializeStorageAccountQueue(App.Current.ApplicationConfiguration.StorageConnectionString);



            BluetoothManager = new BluetoothManager.BluetoothManager();
            BluetoothManager.WeightReceived += UpdateWeight;



            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(7);
            timer.Tick += CheckWeight;
            timer.Start();
            //InitializeStorageAccountQueue(App.Current.ApplicationConfiguration.StorageConnectionString, "weightsesorqueue");
            //WriteToQueue("1234-12-12345678-12.1");
        }

        private async void CheckWeight(object sender, object e)
        {

            if (BluetoothManager.State == RaspPiHub.BluetoothManager.BluetoothManager.BluetoothConnectionState.Connected)
            {
                await BluetoothManager.SendMessageAsync("C:Verify");
            }
        }
        private void SendWeightToEventHub(string sensorData)
        {
            eventHubSender.Send("1234," + sensorData + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
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



        private void UpdateWeight(object sender, double weight)
        {
            SendWeightToEventHub(weight.ToString());
            textBlock.Text = weight.ToString();
        }
        private async void InitializeStorageAccountQueue(string storageAccountString)
        {
            try
            {
                if (storageAccount == null)
                    storageAccount = CloudStorageAccount.Parse(storageAccountString);
                if (queueClient == null)
                    queueClient = storageAccount.CreateCloudQueueClient();
                if (queue == null)
                    queue = queueClient.GetQueueReference("weightsensorqueue");
                await queue.CreateIfNotExistsAsync();
            }
            catch(Exception ex)
            {

            }
        }
        private async void WriteToQueue(string sensorData)
        {
            CloudQueueMessage message = new CloudQueueMessage(sensorData);
            queue.AddMessageAsync(message);
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {

            await BluetoothManager.ConnectToDevice("HC-06");
            
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
