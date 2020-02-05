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

        private void OnPropertyChanging([CallerMemberName]string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IMqttPublisher _publisher;
        private string _topic;
        private int _halfUpDownTimeSeconds;
        private Timer _timer;
        private ShutterStatus _newStatus = ShutterStatus.Unknown;

        public RollerShutterViewModel(string name, IMqttPublisher publisher, IMqttSubscriber subscriber, string topic, int halfUpDownTimeSeconds)
        {
            Name = name;
            _publisher = publisher;
            _topic = topic;
            _halfUpDownTimeSeconds = halfUpDownTimeSeconds;
            _timer = new Timer((state) => 
            {
                System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Timer changing status from {Status} to {_newStatus} | \r\n");
                Status = _newStatus;
                _newStatus = ShutterStatus.Unknown;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }, null, Timeout.Infinite, Timeout.Infinite);

            subscriber.MessageReceived += MessageReceived;
            subscriber.Subscribe(SubscribeTopic);
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Topic != SubscribeTopic)
            {
                return;
            }

            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (e.Payload == "STOPPED")
            {
                IsDisabled = true;
                if (_newStatus != ShutterStatus.Unknown)
                {
                    // we were just opening or closing
                    System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Stopped and disabled, changing status from {Status} to {ShutterStatus.Half} | \r\n");
                    Status = ShutterStatus.Half;
                    _newStatus = ShutterStatus.Unknown;
                }
                if (Status == ShutterStatus.Unknown)
                {
                    System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Stopped and disabled, changing status from {Status} to {ShutterStatus.Half} | \r\n");
                    Status = ShutterStatus.Half;
                }
                return;
            }
            if (IsDisabled)
            {
                System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} reenabled | \r\n");
            }
            IsDisabled = false;

            _newStatus = Payload2ShutterStatus(e.Payload);
            switch (_status)
            {
                case ShutterStatus.Open:
                case ShutterStatus.Opening:
                    if (_newStatus == ShutterStatus.Closed)
                    {
                        System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Changing status from {Status} to {ShutterStatus.Closing}, should become {_newStatus} in {2 * _halfUpDownTimeSeconds} s \r\n");
                        Status = ShutterStatus.Closing;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(2 * _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                case ShutterStatus.Closed:
                case ShutterStatus.Closing:
                    if (_newStatus == ShutterStatus.Open)
                    {
                        System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Changing status from {Status} to {ShutterStatus.Opening}, should become {_newStatus} in {2 * _halfUpDownTimeSeconds} s \r\n");
                        Status = ShutterStatus.Opening;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(2 * _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                case ShutterStatus.Half:
                    if (_newStatus == ShutterStatus.Closed)
                    {
                        System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Changing status from {Status} to {ShutterStatus.Closing}, should become {_newStatus} in {_halfUpDownTimeSeconds} s \r\n");
                        Status = ShutterStatus.Closing;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds( _halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    if (_newStatus == ShutterStatus.Open)
                    {
                        System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Changing status from {Status} to {ShutterStatus.Opening}, should become {_newStatus} in {_halfUpDownTimeSeconds} s \r\n");
                        Status = ShutterStatus.Opening;
                        _timer.Change((int)Math.Round(TimeSpan.FromSeconds(_halfUpDownTimeSeconds).TotalMilliseconds), Timeout.Infinite);
                    }
                    break;
                default:
                    System.Diagnostics.Debugger.Log(1, "", $"Timestamp: {DateTime.Now:O} {Name} Changing status from {Status} to {_newStatus} \r\n");
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
            }
        }

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
                case "OPENED":
                case "OPEN":
                    return ShutterStatus.Open;
                case "CLOSED":
                    return ShutterStatus.Closed;
                default:
                    return ShutterStatus.Unknown;
            }
        }
    }
}
