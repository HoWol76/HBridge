using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Shutters
{
    internal class RollerShuttersViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private TaskScheduler m_taskScheduler;
        private SafeMqttClient mClient;

        private static string nucIP = "192.168.1.105";
        private int port = 1883;

        private const string TOPIC_STATUS = "home/shutter/status";

        private static Dictionary<string, Brush> _statusToBrush = new Dictionary<string, Brush>();

        static RollerShuttersViewModel()
        {
            _statusToBrush["ONLINE"] = Brushes.LightGreen;
            _statusToBrush["CONNECTION LOST"] = Brushes.LightSalmon;
            _statusToBrush["OFFLINE"] = Brushes.LightGray;
        }

        public RollerShuttersViewModel()
        {
            m_taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            mClient = new SafeMqttClient(nucIP, port);
            mClient.StatusChanged += mqttStatusChanged;
            mClient.MessageReceived += mqttMessageReceived;

            Master = new RollerShutterViewModel("Schlafzimmer", mClient, mClient, "home/shutter/master", 7);
            Living = new RollerShutterViewModel("Wohnzimmer", mClient, mClient, "home/shutter/living", 11);
            Stairs = new RollerShutterViewModel("Treppe", mClient, mClient, "home/shutter/stairs", 9);
            Sarah = new RollerShutterViewModel("Sarah", mClient, mClient, "home/shutter/sarah", 7);
            SarahNorth = new RollerShutterViewModel("Sarah Nord", mClient, mClient, "home/shutter/sarah2", 6);
            Toby = new RollerShutterViewModel("Tobi", mClient, mClient, "home/shutter/tobi", 7);

            _ = mClient.startClient();
        }

        private void mqttStatusChanged(object sender, StatusEventArgs e)
        {
            if (e.Status == "Connected")
            {
                var task = mClient.Subscribe(TOPIC_STATUS);
                task.ContinueWith(t =>
                {
                    Status = "Error";
                    StatusBackground = Brushes.Red;
                    Logger.Log($"Exception subscribing to status: {t.Exception}");
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                m_taskScheduler);
                task.ContinueWith(t =>
                {
                    if (_status == "Connecting")
                    {
                        Status = "WAITING FOR SHUTTERS";
                        StatusBackground = Brushes.White;
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                m_taskScheduler);
            }
        }

        private void mqttMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Topic != TOPIC_STATUS)
            {
                return;
            }
            Status = e.Payload;
            if (e.Payload == null)
            {
                StatusBackground = Brushes.LightGray;
            }
            if (_statusToBrush.ContainsKey(e.Payload))
            {
                StatusBackground = _statusToBrush[e.Payload];
            }
            else
            {
                StatusBackground = Brushes.White;
            }
        }

        private string _status = "Connecting";
        public string Status
        {
            get => _status;
            set
            {
                if (_status == value)
                {
                    return;
                }
                _status = value;
                OnPropertyChanged();
            }
        }

        private Brush _statusBackground = Brushes.White;
        public Brush StatusBackground
        {
            get => _statusBackground;
            set
            {
                if (_statusBackground == value)
                {
                    return;
                }
                _statusBackground = value;
                OnPropertyChanged();
            }
        }

        public RollerShutterViewModel Master { get; private set; }

        public RollerShutterViewModel Living { get; private set; }

        public RollerShutterViewModel Stairs { get; private set; }

        public RollerShutterViewModel Sarah { get; private set; }

        public RollerShutterViewModel SarahNorth { get; private set; }

        public RollerShutterViewModel Toby { get; private set; }

        public void Dispose()
        {
            try
            {
                mClient.Dispose();
            }
            catch { }
        }
    }
}
