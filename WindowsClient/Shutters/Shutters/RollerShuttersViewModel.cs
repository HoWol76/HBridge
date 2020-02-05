using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Shutters
{
    internal class RollerShuttersViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private IMqttClient mqttClientPublisher;

        private IMqttClient mqttClientSubscriber;

        private static string nucIP = "192.168.1.105";
        private int port = 1883;

        private const string TOPIC_STATUS = "home/shutter/status";
        private const string TOPIC_LIVING = "home/shutter/living/status";
        private const string TOPIC_STAIRS = "home/shutter/stairs/status";
        private const string TOPIC_MASTER = "home/shutter/master/status";
        private const string TOPIC_SARAH = "home/shutter/sarah/status";

        private static Dictionary<string, Brush> _statusToBrush = new Dictionary<string, Brush>();

        private static Dictionary<string, string> _nameToTopic = new Dictionary<string, string>();

        static RollerShuttersViewModel()
        {
            _statusToBrush["ONLINE"] = Brushes.LightGreen;
            _statusToBrush["CONNECTION LOST"] = Brushes.LightSalmon;
            _statusToBrush["OFFLINE"] = Brushes.LightGray;

            _nameToTopic["Wohnzimmer"] = TOPIC_LIVING.Replace("/status", "/request");
            _nameToTopic["Schlafzimmer"] = TOPIC_MASTER.Replace("/status", "/request");
            _nameToTopic["Stairs"] = TOPIC_STAIRS.Replace("/status", "/request");
            _nameToTopic["Sarah"] = TOPIC_SARAH.Replace("/status", "/request");
        }

        public RollerShuttersViewModel()
        {
            _ = startClient();
        }

        private async Task startClient()
        { 
            try
            {
                await startSubscribing();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debugger.Log(1, "", $"subscribe connect exception: {ex.ToString()}\r\n");
                Status = $"subscribe connect exception: {ex.Message}";
            }
            try
            {
                await subscribe(TOPIC_STATUS );
                await subscribe(TOPIC_LIVING);
                await subscribe(TOPIC_STAIRS);
                await subscribe(TOPIC_MASTER);
                await subscribe(TOPIC_SARAH);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Log(1, "", $"subscribing exception: {ex.ToString()}\r\n");
                Status = $"subscribe exception: {ex.Message}";
            }
            try
            {
                await startPublishing();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Log(1, "", $"publish connect exception: {ex.ToString()}\r\n");
                Status = $"publish connect exception: {ex.Message}";
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

        private ShutterStatus _masterStatus = ShutterStatus.Unknown;
        public ShutterStatus MasterStatus
        {
            get => _masterStatus;
            set
            {
                if (_masterStatus == value)
                {
                    return;
                }
                _masterStatus = value;
                OnPropertyChanged();
            }
        }

        private ShutterStatus _livingStatus = ShutterStatus.Unknown;
        public ShutterStatus LivingStatus
        {
            get => _livingStatus;
            set
            {
                if (_livingStatus == value)
                {
                    return;
                }
                _livingStatus = value;
                OnPropertyChanged();
            }
        }

        private ShutterStatus _stairsStatus = ShutterStatus.Unknown;
        public ShutterStatus StairsStatus
        {
            get => _stairsStatus;
            set
            {
                if (_stairsStatus == value)
                {
                    return;
                }
                _stairsStatus = value;
                OnPropertyChanged();
            }
        }

        private ShutterStatus _sarahStatus = ShutterStatus.Unknown;
        public ShutterStatus SarahStatus
        {
            get => _sarahStatus;
            set
            {
                if (_sarahStatus == value)
                {
                    return;
                }
                _sarahStatus = value;
                OnPropertyChanged();
            }
        }

        private async Task startPublishing()
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = "ClientPublisher",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = nucIP,
                    Port = port,
                    TlsOptions = tlsOptions
                }
            };

           //options.Credentials = new MqttClientCredentials
           //{
           //    Username = "username",
           //    Password = Encoding.UTF8.GetBytes("password")
           //};

            options.CleanSession = true;
            //options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

            this.mqttClientPublisher = mqttFactory.CreateMqttClient();
            Action<MqttApplicationMessageReceivedEventArgs> receivedHandler = HandleReceivedApplicationMessage;
            Action<MqttClientConnectedEventArgs> connecteedHandler = OnPublisherConnected;
            Action<MqttClientDisconnectedEventArgs> disconnectedHandler = OnPublisherDisconnected;
            this.mqttClientPublisher.UseApplicationMessageReceivedHandler(receivedHandler);
            this.mqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(connecteedHandler);
            this.mqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(disconnectedHandler);

            var result = await this.mqttClientPublisher.ConnectAsync(options);
        }

        private void HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {eventArgs.ApplicationMessage.Topic} | Payload: {eventArgs.ApplicationMessage.ConvertPayloadToString()} | QoS: {eventArgs.ApplicationMessage.QualityOfServiceLevel}";
        }

        private void OnPublisherConnected(MqttClientConnectedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Publisher Connected";
            System.Diagnostics.Debugger.Log(1, "", $"{item}\r\n");
        }

        private void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Publisher Disonnected";
            System.Diagnostics.Debugger.Log(1, "", $"{item}\r\n");
        }

        private static void OnSubscriberConnected(MqttClientConnectedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Subscriber Connected";
            System.Diagnostics.Debugger.Log(1, "", $"{item}\r\n");
        }

        private static void OnSubscriberDisconnected(MqttClientDisconnectedEventArgs x)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Subscriber Disonnected";
            System.Diagnostics.Debugger.Log(1, "", $"{item}\r\n");
        }

        private void ButtonGeneratePublishedMessageClick(object sender, EventArgs e)
        {
            var message = $"{{\"dt\":\"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}\"}}";
        }

        public async Task Open(string shutter)
        {
            if (!_nameToTopic.ContainsKey(shutter))
            {
                return;
            }
            await publish("OPEN", _nameToTopic[shutter]);
        }

        public async Task Close(string shutter)
        {
            if (!_nameToTopic.ContainsKey(shutter))
            {
                return;
            }
            await publish("CLOSE", _nameToTopic[shutter]);
        }

        public async Task Stop(string shutter)
        {
            if (!_nameToTopic.ContainsKey(shutter))
            {
                return;
            }
            await publish("STOP", _nameToTopic[shutter]);
        }

        public async Task Half(string shutter)
        {
            if (!_nameToTopic.ContainsKey(shutter))
            {
                return;
            }
            await publish("HALF", _nameToTopic[shutter]);
        }

        private async Task publish(string payload, string topic)
        {
            try
            {
                var utf8Payload = Encoding.UTF8.GetBytes(payload);
                var message = new MqttApplicationMessageBuilder().WithTopic(topic.Trim()).WithPayload(utf8Payload).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag().Build();

                if (this.mqttClientPublisher != null)
                {
                    var result = await this.mqttClientPublisher.PublishAsync(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Log(1, "", $"publishing {payload} (topic {topic}): {ex.ToString()}\r\n");
            }
        }

        private async Task subscribe(string topic)
        {
            await this.mqttClientSubscriber.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic.Trim()).Build());
        }

        private async Task startSubscribing()
        {
            var mqttFactory = new MqttFactory();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = "ClientSubscriber",
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = nucIP,
                    Port = port,
                    TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.CleanSession = true;
            //options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

            this.mqttClientSubscriber = mqttFactory.CreateMqttClient();
            Action<MqttClientConnectedEventArgs> connectAction = OnSubscriberConnected;
            Action<MqttClientDisconnectedEventArgs> disonnectAction = OnSubscriberDisconnected;
            Action<MqttApplicationMessageReceivedEventArgs> receiveSubscribeAction = OnSubscriberMessageReceived;
            this.mqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(connectAction);
            this.mqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(disonnectAction);
            this.mqttClientSubscriber.UseApplicationMessageReceivedHandler(receiveSubscribeAction);

            await this.mqttClientSubscriber.ConnectAsync(options);
        }

        public void Dispose()
        {
            if (this.mqttClientPublisher != null)
            {
                this.mqttClientPublisher.DisconnectAsync().Wait();
                this.mqttClientPublisher = null;
            }
            if (this.mqttClientSubscriber != null)
            {
                this.mqttClientSubscriber.DisconnectAsync().Wait();
                this.mqttClientSubscriber = null;
            }
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
                case "STOPPED":
                    return ShutterStatus.Stopped;
                default:
                    return ShutterStatus.Unknown;
            }
        }

        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            var topic = x.ApplicationMessage.Topic;
            var payload = x.ApplicationMessage.ConvertPayloadToString();
            switch(topic)
            {
                case TOPIC_STATUS   :
                    Status = payload;
                    if (_statusToBrush.ContainsKey(Status))
                    {
                        StatusBackground = _statusToBrush[Status];
                    }
                    else
                    {
                        StatusBackground = Brushes.White;
                    }
                    break;
                case TOPIC_LIVING   :
                    LivingStatus = Payload2ShutterStatus(payload);
                    break;
                case TOPIC_STAIRS   :
                    StairsStatus = Payload2ShutterStatus(payload);
                    break;
                case TOPIC_MASTER   :
                    MasterStatus = Payload2ShutterStatus(payload);
                    break;
                case TOPIC_SARAH    :
                    SarahStatus = Payload2ShutterStatus(payload);
                    break;
            }
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {x.ApplicationMessage.Topic} | Payload: {x.ApplicationMessage.ConvertPayloadToString()} | QoS: {x.ApplicationMessage.QualityOfServiceLevel}";
            System.Diagnostics.Debugger.Log(1, "", $"{item}\r\n");
        }
    }
}
