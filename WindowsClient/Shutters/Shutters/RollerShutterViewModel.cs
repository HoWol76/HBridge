using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Shutters
{
    internal class RollerShutterViewModel : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanging([CallerMemberName]string propertyName ="")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName ="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IMqttPublisher _publisher;
        private IMqttSubscriber _subscriber;
        private string _topic;
        private int _halfUpDownTimeSeconds;
        private Timer _timer;
        private ShutterStatus _newStatus = ShutterStatus.Unknown;

        public RollerShutterViewModel(string name, IMqttPublisher publisher, IMqttSubscriber subscriber, string topic, int halfUpDownTimeSeconds)
        {
            Name = name;
            _publisher = publisher;
            _subscriber = subscriber;
            _topic = topic;
            _halfUpDownTimeSeconds = halfUpDownTimeSeconds;
            _timer = new Timer((state) => 
            {
                Logger.Log($"{Name} Timer changing status from {Status} to {_newStatus}");
                Status = _newStatus;
                _newStatus = ShutterStatus.Unknown;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }, null, Timeout.Infinite, Timeout.Infinite);

            _subscriber.MessageReceived += MessageReceived;
            _subscriber.StatusChanged += SubscriberStatusChanged;
        }

        private void SubscriberStatusChanged(object sender, StatusEventArgs e)
        {
            try
            {
                if (e.Status == "Connected")
                {
                    Logger.Log($"{Name} connected");
                    _subscriber.Subscribe(SubscribeTopic);
                }
                else
                {
                    Logger.Log($"{Name} disconnected");
                    _subscriber.Unsubscribe(SubscribeTopic);
                }
            }
            catch(Exception ex)
            {
                Status = ShutterStatus.Error;
            }
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Topic != SubscribeTopic)
            {
                return;
            }

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (e.Payload =="STOPPED")
            {
                IsDisabled = true;
                if (_newStatus != ShutterStatus.Unknown)
                {
                    // we were just opening or closing
                    Logger.Log($"{Name} Stopped and disabled, changing status from {Status} to {ShutterStatus.Half}");
                    Status = ShutterStatus.Half;
                    _newStatus = ShutterStatus.Unknown;
                }
                if (Status == ShutterStatus.Unknown)
                {
                    Logger.Log($"{Name} Stopped and disabled, changing status from {Status} to {ShutterStatus.Half}");
                    Status = ShutterStatus.Half;
                }
                return;
            }
            if (IsDisabled)
            {
                Logger.Log($"{Name} reenabled");
            }
            IsDisabled = false;

            _newStatus = Payload2ShutterStatus(e.Payload);
            switch (_status)
            {
                case ShutterStatus.Open:
                case ShutterStatus.Opening:
                    if (_newStatus == ShutterStatus.Closed)
                    {
                        Logger.Log($"{Name} Changing status from {Status} to {ShutterStatus.Closing}, should become {_newStatus} in {2 * _halfUpDownTimeSeconds} s");
                        Status = ShutterStatus.Closing;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(2 * _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                case ShutterStatus.Closed:
                case ShutterStatus.Closing:
                    if (_newStatus == ShutterStatus.Open)
                    {
                        Logger.Log($"{Name} Changing status from {Status} to {ShutterStatus.Opening}, should become {_newStatus} in {2 * _halfUpDownTimeSeconds} s");
                        Status = ShutterStatus.Opening;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(2 * _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                case ShutterStatus.Half:
                    if (_newStatus == ShutterStatus.Closed)
                    {
                        Logger.Log($"{Name} Changing status from {Status} to {ShutterStatus.Closing}, should become {_newStatus} in {_halfUpDownTimeSeconds} s");
                        Status = ShutterStatus.Closing;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds( _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    if (_newStatus == ShutterStatus.Open)
                    {
                        Logger.Log($"{Name} Changing status from {Status} to {ShutterStatus.Opening}, should become {_newStatus} in {_halfUpDownTimeSeconds} s");
                        Status = ShutterStatus.Opening;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(_halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                default:
                    Logger.Log($"{Name} Changing status from {Status} to {_newStatus}");
                    Status = _newStatus;
                    _newStatus = ShutterStatus.Unknown;
                    break;
            }

        }

        public string Name { get; private set; }

        private string PublishTopic { get => $"{_topic}/request"; }

        private string SubscribeTopic { get => $"{_topic}/status"; }

        bool _isDisabled = false;
        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                _isDisabled = value;
                OnPropertyChanged();
            }
        }

        private ShutterStatus _status;
        public ShutterStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextBrush));
            }
        }

        public Brush TextBrush => _status == ShutterStatus.Error ? Brushes.Red : _status == ShutterStatus.Unknown ? Brushes.LightGray : IsDisabled ? Brushes.Gray : Brushes.Black;

        public async Task Open()
        {
            await _publisher.publish("OPEN", PublishTopic);
        }

        public async Task Close()
        {
            await _publisher.publish("CLOSE", PublishTopic);
        }

        public async Task Stop()
        {
            await _publisher.publish("STOP", PublishTopic);
        }

        public async Task Half()
        {
            await _publisher.publish("HALF", PublishTopic);
        }

        private ShutterStatus Payload2ShutterStatus(string payload)
        {
            switch (payload)
            {
                case"OPENED":
                case"OPEN":
                    return ShutterStatus.Open;
                case"CLOSED":
                    return ShutterStatus.Closed;
                default:
                    return ShutterStatus.Unknown;
            }
        }
    }
}
