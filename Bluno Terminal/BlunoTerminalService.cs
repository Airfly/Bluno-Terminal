using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace Bluno_Terminal
{
    public delegate void ValueChangeCompletedHandler(string value);

    public delegate void SendCompletedHandler(bool successed, string message);

    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);

    public delegate void ServiceNotifiedHandler(string notifyInfo);

    public class BlunoTerminalService
    {
        //private DeviceInformation _device = null;
        private int _currentCharacteristicId = 1; //1-serial service; 2-command service;

        // Bluno Terminal Constants

        // The Characteristic we want to obtain getting & sending data for is the Bluno Terminal Service characteristic
        private Guid SERIAL_CHARACTERISTIC_UUID = GattCharacteristic.ConvertShortIdToUuid(0xDFB1);
        private Guid COMMAND_CHARACTERISTIC_UUID = GattCharacteristic.ConvertShortIdToUuid(0xDFB2);

        // Bluno Terminal devices typically have only one Bluno Terminal Service characteristic.
        // Make sure to check your device's documentation to find out how many characteristics your specific device has.
        private const int CHARACTERISTIC_INDEX = 0;
        // The Bluno Terminal Profile specification requires that the Bluno Terminal Service characteristic is notifiable.
        //private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE =
        //    GattClientCharacteristicConfigurationDescriptorValue.Notify;

        private static BlunoTerminalService instance = new BlunoTerminalService();
        private GattDeviceService _service;
        private GattCharacteristic _characteristicSerial, _characteristicCommand;

        private PnpObjectWatcher _watcher;
        private String _deviceContainerId;

        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event SendCompletedHandler SendCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;
        public event ServiceNotifiedHandler ServiceNotified;

        public static BlunoTerminalService Instance
        {
            get { return instance; }
        }

        public bool IsServiceInitialized { get; set; }

        public GattDeviceService Service
        {
            get { return _service; }
        }

        private BlunoTerminalService()
        {
            App.Current.Suspending += App_Suspending;
            App.Current.Resuming += App_Resuming;
        }

        private void App_Resuming(object sender, object e)
        {
            // Since the Windows Runtime will close resources to the device when the app is suspended,
            // the device needs to be reinitialized when the app is resumed.
        }

        private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            IsServiceInitialized = false;

            // Allow the GattDeviceService to get cleaned up by the Windows Runtime.
            // The Windows runtime will clean up resources used by the GattDeviceService object when the application is
            // suspended. The GattDeviceService object will be invalid once the app resumes, which is why it must be 
            // marked as invalid, and reinitalized when the application resumes.
            if (_service != null)
            {
                _service.Dispose();
                _service = null;
            }

            if (_characteristicSerial != null)
            {
                _characteristicSerial = null;
            }

            if (_characteristicCommand != null)
            {
                _characteristicCommand = null;
            }

            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher = null;
            }

            //if (_device != null)
            //{
            //    _device = null;
            //}
        }

        public async Task InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                //_device = device;

                _deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";

                _service = await GattDeviceService.FromIdAsync(device.Id);
                if (_service != null)
                {
                    IsServiceInitialized = true;
                    await ConfigureServiceForNotificationsAsync();
                }
                else
                {
                    if (ServiceNotified != null)
                    {
                        ServiceNotified("Access to the device is denied, because the application was not granted access, " +
                            "or the device is currently in use by another application.");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ServiceNotified != null)
                {
                    ServiceNotified("ERROR: Accessing your device failed." + Environment.NewLine + ex.Message);
                }
            }
        }

        public async void Send(string value)
        {
            try
            {
                if (_currentCharacteristicId == 1)
                {
                    await SendData(_characteristicSerial, value);
                }
                else
                {
                    await SendData(_characteristicCommand, value);
                }
            }
            catch (Exception ex)
            {
                //DataSendedEvent(0);
                if (ex.InnerException != null)
                    SendCompleted(false, value + "  ERROR: " + ex.InnerException.Message);
                else
                    SendCompleted(false, value + "  ERROR: " + ex.Message);
            }
        }

        private async Task SendData(GattCharacteristic characteristic, string content)
        {
            string strSending = content;
            if (_currentCharacteristicId == 2)
            {
                //Sending AT command end with  \r\n
                if (!strSending.EndsWith("\r\n"))
                {
                    strSending += "\r\n";
                }
            }

            try
            {
                Windows.Storage.Streams.InMemoryRandomAccessStream im = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                Windows.Storage.Streams.DataWriter writer = new Windows.Storage.Streams.DataWriter(im);
                writer.WriteString(strSending);
                var y = writer.DetachBuffer();
                var x = await characteristic.WriteValueAsync(y, GattWriteOption.WriteWithoutResponse);
                if (x == GattCommunicationStatus.Unreachable)
                {
                    throw new InvalidOperationException("Device unreachable");
                }
                else if (x == GattCommunicationStatus.Success)
                {
                    //DataSendedEvent(strSending.Length);
                    SendCompleted(true, strSending);
                }
            }
            catch (Exception ex)
            {
                //DataSendedEvent(0);
                if (ex.InnerException != null)
                    SendCompleted(false, strSending + "  ERROR: " + ex.InnerException.Message);
                else
                    SendCompleted(false, strSending + "  ERROR: " + ex.Message);
            }
            finally
            {
                if (_currentCharacteristicId == 2 && strSending.Equals("AT+EXIT\r\n"))
                {
                    SwitchAtMode(false);
                }
            }
        }

        public void SwitchAtMode(bool value)
        {
            if ((value && _currentCharacteristicId == 2) || (!value && _currentCharacteristicId == 1))
                return;

            try
            {
                if (value)
                {
                    _currentCharacteristicId = 2;

                    _characteristicSerial.ValueChanged -= Characteristic_ValueChanged;
                    _characteristicCommand.ValueChanged += Characteristic_ValueChanged;

                    //SendCommand(_characteristicCommand, "AT+PASSWORD=DFRobot");
                }
                else
                {
                    _currentCharacteristicId = 1;

                    _characteristicCommand.ValueChanged -= Characteristic_ValueChanged;
                    _characteristicSerial.ValueChanged += Characteristic_ValueChanged;
                }
            }
            catch (Exception ex)
            {
                //DataSendedEvent(0);
                if (ex.InnerException != null)
                    SendCompleted(false, "+++  ERROR: " + ex.InnerException.Message);
                else
                    SendCompleted(false, "+++  ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Configure the Bluetooth device to send notifications whenever the Characteristic value changes
        /// </summary>
        private async Task ConfigureServiceForNotificationsAsync()
        {
            //GattReadClientCharacteristicConfigurationDescriptorResult currentDescriptorValue = null;
            //bool isDone = false;
            try
            {
                // Obtain the characteristic for which notifications are to be received
                _characteristicSerial = _service.GetCharacteristics(SERIAL_CHARACTERISTIC_UUID)[CHARACTERISTIC_INDEX];
                _characteristicCommand = _service.GetCharacteristics(COMMAND_CHARACTERISTIC_UUID)[CHARACTERISTIC_INDEX];

                // While encryption is not required by all devices, if encryption is supported by the device,
                // it can be enabled by setting the ProtectionLevel property of the Characteristic object.
                // All subsequent operations on the characteristic will work over an encrypted link.
                _characteristicSerial.ProtectionLevel = GattProtectionLevel.EncryptionRequired;

                // Register the event handler for receiving notifications
                _characteristicSerial.ValueChanged += Characteristic_ValueChanged;

                // In order to avoid unnecessary communication with the device, determine if the device is already 
                // correctly configured to send notifications.
                // By default ReadClientCharacteristicConfigurationDescriptorAsync will attempt to get the current
                // value from the system cache and communication with the device is not typically required.
                //var currentDescriptorValue = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();
                //2014.12.27 this will give an exception during operation on Bluno

                //if ((currentDescriptorValue.Status != GattCommunicationStatus.Success) ||
                //    (currentDescriptorValue.ClientCharacteristicConfigurationDescriptor !=
                //     CHARACTERISTIC_NOTIFICATION_TYPE))
                //{
                    // Set the Client Characteristic Configuration Descriptor to enable the device to send notifications
                    // when the Characteristic value changes
                    //GattCommunicationStatus status =
                    //    await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    //        CHARACTERISTIC_NOTIFICATION_TYPE);
                    ////2014.12.27 this will give an exception during operation on Bluno

                    //if (status == GattCommunicationStatus.Unreachable)
                    //{
                    //    // Register a PnpObjectWatcher to detect when a connection to the device is established,
                    //    // such that the application can retry device configuration.
                    //    StartDeviceConnectionWatcher();
                    //}
                //}

                //isDone = true;
            }
            catch (Exception ex)
            {
                if (ServiceNotified != null)
                {
                    ServiceNotified("ERROR: Accessing your device failed." + Environment.NewLine + ex.Message);
                }
            }
            finally
            {
                //if (!isDone)
                //{
                    StartDeviceConnectionWatcher();
                //}
            }
        }

        /// <summary>
        /// Register to be notified when a connection is established to the Bluetooth device
        /// </summary>
        private void StartDeviceConnectionWatcher()
        {
            _watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" },String.Empty);

            /*/ Check if the device is initially connected, and display the appropriate message to the user
            var deviceObject = await PnpObject.CreateFromIdAsync(PnpObjectType.DeviceContainer,
                _device.Properties["System.Devices.ContainerId"].ToString(),
                new string[] { "System.Devices.Connected" });

            bool isConnected;
            if (Boolean.TryParse(deviceObject.Properties["System.Devices.Connected"].ToString(), out isConnected))
            {
                OnDeviceConnectionUpdated(isConnected);
            }*/

            _watcher.Updated += DeviceConnection_Updated;
            _watcher.Start();
        }

        /// <summary>
        /// Invoked when a connection is established to the Bluetooth device
        /// </summary>
        /// <param name="sender">The watcher object that sent the notification</param>
        /// <param name="args">The updated device object properties</param>
        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var connectedProperty = args.Properties["System.Devices.Connected"];
            bool isConnected = false;
            if ((_deviceContainerId == args.Id) && Boolean.TryParse(connectedProperty.ToString(), out isConnected) &&
                isConnected)
            {
                try
                {
                    //2014.12.27 this will give an exception during operation on Bluno
                    //var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    //CHARACTERISTIC_NOTIFICATION_TYPE);

                    //if (status == GattCommunicationStatus.Success)
                    //{
                        IsServiceInitialized = true;

                        // Once the Client Characteristic Configuration Descriptor is set, the watcher is no longer required
                        _watcher.Updated -= DeviceConnection_Updated;
                        _watcher.Stop();
                        _watcher = null;
                    //}
                }
                catch (Exception ex)
                {
                    if (ServiceNotified != null)
                    {
                        ServiceNotified("ERROR: Accessing your device failed." + Environment.NewLine + ex.Message);
                    }
                }
            }

            // Notifying subscribers of connection state updates
            if (DeviceConnectionUpdated != null)
            {
                DeviceConnectionUpdated(isConnected);
            }
        }

        /// <summary>
        /// Invoked when Windows receives data from your Bluetooth device.
        /// </summary>
        /// <param name="sender">The GattCharacteristic object whose value is received.</param>
        /// <param name="args">The new characteristic value sent by the device.</param>
        private void Characteristic_ValueChanged(
            GattCharacteristic sender,
            GattValueChangedEventArgs args)
        {
            /*var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            // Process the raw data received from the device.
            var value = ProcessData(data);
            value.Timestamp = args.Timestamp;*/

            if ((_currentCharacteristicId == 1 && sender == _characteristicCommand) ||
                (_currentCharacteristicId == 2 && sender == _characteristicSerial))
                return;

            using (DataReader reader = DataReader.FromBuffer(args.CharacteristicValue))
            {
                string strReceived = reader.ReadString(args.CharacteristicValue.Length);

                if (!string.IsNullOrEmpty(strReceived))
                {
                    if (ValueChangeCompleted != null)
                    {
                        ValueChangeCompleted(strReceived);
                    }
                }
            }
        }

    }
}
