using System;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Bluno_Terminal
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            //this.NavigationCacheMode = NavigationCacheMode.Required;

            #region UI resize
            double winWidth = Windows.UI.Xaml.Window.Current.Bounds.Width;
            double winHeidht = Windows.UI.Xaml.Window.Current.Bounds.Height;

            txtTitle.Width = winWidth;
            txtTitle.Height = 110;
            txtTitle.Margin = new Thickness(0,0,0,winHeidht - 110);

            lstDevices.Width = (winWidth - 20) /2;
            lstDevices.Height = winHeidht - 110;
            lstDevices.Margin = new Thickness(2, 110, winWidth / 2 + 10, 2);

            txtSend.Width = (winWidth - 20) / 2 - 92;
            txtSend.Height = 40;
            txtSend.Margin = new Thickness(winWidth / 2 + 10, 110, 92, winHeidht - 154);

            //btnSend.Width = (winWidth - 20) / 2;
            btnSend.Height = 40;
            btnSend.Margin = new Thickness(winWidth - 90, 110, 2, winHeidht - 154);

            txtLog.Width = (winWidth - 20) / 2;
            txtLog.Height = winHeidht - 150;
            txtLog.Margin = new Thickness(winWidth / 2 + 10, 154, 2, 2);
            #endregion

            RefreshDeviceList();
        }

        #region UI works
        private async void RefreshDeviceList()
        {
            //GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.GenericAccess)
            var devices = await DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromShortId(0xDFB0),
                new string[] { "System.Devices.ContainerId" });

            if (lstDevices.Items != null) lstDevices.Items.Clear();

            if (devices.Count > 0)
            {
                lstDevices.ItemsSource = devices;
            }
            else
            {
                await new MessageDialog("No Bluno devices were found or bluetooth disabled. Please make sure your device is paired and powered on!", "Info").ShowAsync();
                //Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:", UriKind.RelativeOrAbsolute));
            }
        }

        private async void lstDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var device = lstDevices.SelectedItem as DeviceInformation;
            if (device == null)
                return;

            ShowLog("Initializing " + device.Name + "...");

            BlunoTerminalService.Instance.DeviceConnectionUpdated += OnDeviceConnectionUpdated;
            BlunoTerminalService.Instance.ServiceNotified += OnServiceNotified;
            BlunoTerminalService.Instance.ValueChangeCompleted += OnValueChangeCompleted;
            BlunoTerminalService.Instance.SendCompleted += OnSendCompleted;

            await BlunoTerminalService.Instance.InitializeServiceAsync(device);

            try
            {
                // Check if the device is initially connected, and display the appropriate message to the user
                var deviceObject = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer,
                    device.Properties["System.Devices.ContainerId"].ToString(),
                    new string[] { "System.Devices.Connected" });

                bool isConnected;
                if (Boolean.TryParse(deviceObject.Properties["System.Devices.Connected"].ToString(), out isConnected))
                {
                    OnDeviceConnectionUpdated(isConnected);
                }
            }
            catch (Exception ex)
            {
                ShowLog("Retrieving device properties failed with message: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            BlunoTerminalService service = BlunoTerminalService.Instance;
            if (txtSend.Text.Length > 0 && service.IsServiceInitialized)
            {
                if (txtSend.Text.Equals("+++"))
                {
                    service.SwitchAtMode(true);
                    txtSend.Text = "AT+PASSWORD=";
                    ShowLog("Please enter your Bluno's pass code atfer 'AT+PASSWORD=',\r\n" + 
                        "default code is DFRobot.\r\nuse 'AT+EXIT' to exit AT mode.\r\n");
                }
                else
                    service.Send(txtSend.Text);
            }
        }

        private async void ShowLog(string text)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                txtLog.Text = text + Environment.NewLine + txtLog.Text;

                if (txtLog.Text.Length > 500)
                    txtLog.Text = txtLog.Text.Substring(0, 500);
            });
        }

        #region Menu Button
        private void AppBarButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
        }

        private void AppBarButtonBluetooth_Click(object sender, RoutedEventArgs e)
        {
            //Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:", UriKind.RelativeOrAbsolute));
        }
        #endregion

        #endregion

        #region Bluno Terminal Service Events
        private async void OnDeviceConnectionUpdated(bool isConnected)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (isConnected)
                {
                    ShowLog("Waiting for device to send data...\r\nConnected...");
                }
                else
                {
                    ShowLog("Waiting for device to connect...");
                }
            });
        }

        private async void OnServiceNotified(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ShowLog(message);
            });
        }

        private async void OnValueChangeCompleted(string value)
        {
            // Serialize UI update to the the main UI thread.
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ShowLog("Received: " + value);
            });
        }

        private async void OnSendCompleted(bool successed, string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (successed)
                {
                    ShowLog("Sent_____: " + message);
                }
                else
                {
                    ShowLog("SentFailed: " + message);
                }
            });
        }

        #endregion
    }
}
