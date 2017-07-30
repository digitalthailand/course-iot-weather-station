using Emmellsoft.IoT.Rpi.SenseHat;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WeatherStation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public ISenseHat SenseHat;
        public DispatcherTimer timer;

        private static readonly string DeviceConnectionString = "{device connection string from Azure IoT Hub}";
        private static readonly string DeviceID = "{device id}";
        private DeviceClient deviceClient;

        private bool shouldOn;

        public MainPage()
        {
            this.InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

            InitSenseHat();
        }

        private async void InitSenseHat()
        {
            ISenseHat senseHat = await SenseHatFactory.GetSenseHat();
            this.SenseHat = senseHat;

            await deviceClient.OpenAsync();

            SenseHat.Display.Clear();
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            BlinkLed();
            DoTelemetry();
        }

        private void BlinkLed()
        {
            shouldOn = !shouldOn;

            if (shouldOn)
            {
                SenseHat.Display.Fill(Colors.Green);
            }
            else
            {
                SenseHat.Display.Fill(Colors.Black);
            }
            SenseHat.Display.Update();
        }

        private void DoTelemetry()
        {
            SenseHat.Sensors.HumiditySensor.Update();
            if (SenseHat.Sensors.Humidity.HasValue
                && SenseHat.Sensors.Temperature.HasValue)
            {
                var temperature = SenseHat.Sensors.Temperature.Value;
                var humidity = SenseHat.Sensors.Humidity.Value;

                SendTelemetry(temperature, humidity);
            }
        }

        public async void SendTelemetry(double temperature, double humidity)
        {
            var telemetryDataPoint = new
            {
                DeviceID = DeviceID,
                Temperature = temperature,
                Humidity = humidity
            };
            await deviceClient.SendEventAsync(ToMessage(telemetryDataPoint));
        }

        private Message ToMessage(object data)
        {
            var jsonText = JsonConvert.SerializeObject(data);
            var dataBuffer = System.Text.UTF8Encoding.UTF8.GetBytes(jsonText);
            return new Message(dataBuffer);
        }
    }
}
