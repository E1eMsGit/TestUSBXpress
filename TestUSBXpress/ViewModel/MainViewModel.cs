using System;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TestUSBXpress.Helpers;
using TestUSBXpress.Model;

namespace TestUSBXpress.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        private string _status;
        private bool _isLed1On;
        private bool _isLed2On;
        private Brush _button1Color;
        private Brush _button2Color;
        private int _temperatureValue;
        private int _potentiometerValue;
        private IDevice _device;
        private byte[] _inBuffer; // buffer for writing data.
        private ManagementEventWatcher _connectWatcher;
        private ManagementEventWatcher _disconnectWatcher;
        private CancellationTokenSource _cancelTokenSource;

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsLed1On
        {
            get => _isLed1On;
            set
            {
                _isLed1On = value;
                if (_device.IsConnected)
                {
                    SwitchLed(1);
                }               
            }
        }
       
        public bool IsLed2On
        {
            get => _isLed2On;
            set
            {
                _isLed2On = value;
                if (_device.IsConnected)
                {
                    SwitchLed(2);
                }               
            }
        }
        
        public Brush Button1Color 
        {
            get => _button1Color;
            set
            {
                _button1Color = value;
                OnPropertyChanged("Button1Color");
            }
        }
        
        public Brush Button2Color
        {
            get => _button2Color;
            set
            {
                _button2Color = value;
                OnPropertyChanged("Button2Color");
            }
        }
        
        public int TemperatureValue
        {
            get => _temperatureValue;
            set
            {
                _temperatureValue = value;
                OnPropertyChanged(nameof(TemperatureValue));
            }
        }
        
        public int PotentiometerValue
        {
            get => _potentiometerValue;
            set
            {
                _potentiometerValue = value;
                OnPropertyChanged(nameof(PotentiometerValue));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand LoadedWindowCommand => new RelayCommand(
            async action =>
            {
                try
                {
                    Status = "Connecting ...";
                    await Task.Run(() => _device.Open());

                    if (_device.IsConnected)
                    {
                        Status = "Device connected";
                        _cancelTokenSource = new CancellationTokenSource();
                        await ReadFromDevice(_cancelTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    Status = ex.Message;
                }
            });
        
        public RelayCommand CloseWindowCommand => new RelayCommand(
            async action =>
            {
                if (_device.IsConnected)
                {
                    _inBuffer[0] = 0;
                    _inBuffer[1] = 0;
                    await _device.WriteToDeviceAsync(_inBuffer);

                    _device.Close();
                }

                Application.Current.Shutdown();
            });

        public MainViewModel()
        {
            _device = new SiliconDevice();
            _inBuffer = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

            Button1Color = Constants.RED_COLOR;
            Button2Color = Constants.RED_COLOR;

            // Watch for the device being connected
            _connectWatcher = new ManagementEventWatcher();
            WqlEventQuery connectQuery = new WqlEventQuery();
            connectQuery.EventClassName = "__InstanceCreationEvent";
            connectQuery.Condition = "TargetInstance ISA 'Win32_USBControllerDevice'";
            connectQuery.WithinInterval = new TimeSpan(0, 0, 1);
            _connectWatcher.Query = connectQuery;
            // Method call when device connected
            _connectWatcher.EventArrived += new EventArrivedEventHandler(HandleDeviceConnect);
            _connectWatcher.Start();

            // Watch for the device being disconnected
            _disconnectWatcher = new ManagementEventWatcher();
            WqlEventQuery disconnectQuery = new WqlEventQuery();
            disconnectQuery.EventClassName = "__InstanceDeletionEvent";
            disconnectQuery.Condition = "TargetInstance ISA 'Win32_USBControllerDevice'";
            disconnectQuery.WithinInterval = new TimeSpan(0, 0, 1);
            _disconnectWatcher.Query = disconnectQuery;
            // Method call when device disconnected
            _disconnectWatcher.EventArrived += new EventArrivedEventHandler(HandleDeviceDisconnect);
            _disconnectWatcher.Start();
        }

        private async Task ReadFromDevice(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _device.ReadFromDeviceAsync().ContinueWith(t =>
                {
                    Button1Color = t.Result[0] > 0 ? Constants.GREEN_COLOR : Constants.RED_COLOR;
                    Button2Color = t.Result[1] > 0 ? Constants.GREEN_COLOR : Constants.RED_COLOR;
                    PotentiometerValue = t.Result[3];
                    TemperatureValue = t.Result[4];
                });

                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        private async void SwitchLed(int ledNumber)
        {
            switch (ledNumber)
            {
                case 1:
                    _inBuffer[0] = (byte)(IsLed1On ? 1 : 0);
                    break;
                case 2:
                    _inBuffer[1] = (byte)(IsLed2On ? 1 : 0);
                    break;
                default:
                    break;
            }

            try
            {
                await _device.WriteToDeviceAsync(_inBuffer);
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private async void HandleDeviceConnect(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (!_device.IsConnected)
                {
                    Status = "Connecting ...";
                    await Task.Run(() => _device.Open());
                }

                Status = "Device connected";
                _cancelTokenSource = new CancellationTokenSource();
                await ReadFromDevice(_cancelTokenSource.Token);
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        }

        private void HandleDeviceDisconnect(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (_device.GetDevices == 0)
                {
                    _device.IsConnected = false;
                }

                Status = "Device disconnected";
                _cancelTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }
        }
    }
}
