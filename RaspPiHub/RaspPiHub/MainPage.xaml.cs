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
using System.Windows;
using Newtonsoft.Json;
using Windows.UI.Popups;
using System.Net;
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

            UpdateDevicesList();


            timer.Interval = TimeSpan.FromSeconds(10);
            timer.Tick += CheckWeight;
            timer.Start();
    
        }

        private async void CheckWeight(object sender, object e)
        {
            timer.Stop();

            foreach (var sensor in App.Current.ApplicationConfiguration.ConnectedSensors)
            {

                //if(BluetoothManager.State == RaspPiHub.BluetoothManager.BluetoothManager.BluetoothConnectionState.Disconnected)
                await BluetoothManager.ConnectToDevice(sensor.BluetoothId, sensor.SensorId);
                if (BluetoothManager.State == RaspPiHub.BluetoothManager.BluetoothManager.BluetoothConnectionState.Connected)
                {
                    
                    await BluetoothManager.SendMessageAsync("W");
                    await BluetoothManager.ListenForMessagesAsync();
                }
                //BluetoothManager.Disconnect();
            }
            timer.Start();

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

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            UpdateDevicesList();
        }
        private async void UpdateDevicesList()
        {
            Devices_List.Items.Clear();

            await BluetoothManager.ScanForDevices();
            foreach (var item in BluetoothManager.DeviceCollection)
            {
                var itemValue = "";
                foreach (var savedSensor in App.Current.ApplicationConfiguration.ConnectedSensors)
                    if (savedSensor.BluetoothId == item.Id)
                        itemValue = "(Added) ";
                itemValue += item.Name + "     " + item.Id;
                Devices_List.Items.Add(itemValue);
            }
        }

        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            AddSensor();
        }
        private async void AddSensor()
        {
            timer.Stop();
            if (Devices_List.SelectedItem != null)
            {
                var data = Devices_List.SelectedItem.ToString().Split(' ');
                var sensorId = data.LastOrDefault();
                var sensorName = data[1];
                if (sensorId != null)
                {
                    foreach (var sensor in App.Current.ApplicationConfiguration.ConnectedSensors)
                    {
                        if (sensor.BluetoothId == sensorId)
                        {

                            UpdateErrorMessage("This sensor is already being tracked.");

                            timer.Start();
                            return;
                        }
                    }
                    var newSensor = new SensorProfile()
                    {
                        SensorId = Guid.NewGuid(),
                        BluetoothId = sensorId,
                        BluetoothName = sensorName
                    };
                    App.Current.ApplicationConfiguration.ConnectedSensors.Add(newSensor);
                    var request = (HttpWebRequest)WebRequest.Create("http://" + App.Current.ApplicationConfiguration.AzureVMIp + "/insert_sensor?sensor_id=" + newSensor.SensorId + "&sensor_name=NewlyAddedSensor");
                    await request.GetResponseAsync();
                    var writeSuccess = await App.Current.SaveApplicationConfiguration();

                    if (!writeSuccess)
                        UpdateErrorMessage("Configuration write failed.");
                    else
                        UpdateDevicesList();
                }

            }
            UpdateErrorMessage("Select a sensor from the list.");
            timer.Start();

        }
        private async void RemoveSensor()
        {
            timer.Stop();
            if (Devices_List.SelectedItem != null)
            {
                var data = Devices_List.SelectedItem.ToString().Split(' ');
                var sensorId = data.LastOrDefault();
                var sensorName = data[1];
                if (sensorId != null)
                {
                    for(int i = 0; i < App.Current.ApplicationConfiguration.ConnectedSensors.Count; i++)
                    {
                        var sensor = App.Current.ApplicationConfiguration.ConnectedSensors.ElementAt(i);
                        if (sensor.BluetoothId == sensorId)
                        {
                            App.Current.ApplicationConfiguration.ConnectedSensors.Remove(sensor);
                            var writeSuccess = await App.Current.SaveApplicationConfiguration();

                            if (!writeSuccess)
                                UpdateErrorMessage("Configuration write failed.");
                            else
                                UpdateDevicesList();
                        }
                    }

                    UpdateErrorMessage("This sensor is not being tracked");
                    timer.Start();
                    return;

                }

            }
            UpdateErrorMessage("Select a sensor from the list.");
            timer.Start();
        }
        private void UpdateErrorMessage(string message)
        {
            ErrorMessage.Text = "This sensor is already being tracked.";
            if (ErrorMessage.Visibility == Visibility.Collapsed)
                ErrorMessage.Visibility = Visibility.Visible;
        }

        private void RemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            RemoveSensor();
        }
    }
}
