using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Shutters
{
    internal class SafeMqttClient: IMqttPublisher, IMqttSubscriber, IDisposable
    {
        public event StatusEventHandler StatusChanged;
        public event MessageEventHandler MessageReceived;

        private IMqttClient mqttPublishClient;
        private IMqttClient mqttSubscribeClient;

        private static string _brokerIP;
        private int _brokerPort = 1883;
        private List<string> _subsribeTopics = new List<string>();
        private object _lock = new object();

        public SafeMqttClient(string brokerIP = "192.168.1.105", int brokerPort = 1883)
        {
            _brokerIP = brokerIP;
            _brokerPort = brokerPort;
            Logger.LogVerbose($"Connecting to MQTT server at {_brokerIP}:{_brokerPort}");
        }

        private string SubscribeClientId { get; set; } = Guid.NewGuid().ToString();
        private string PublishClientId { get; set; } = Guid.NewGuid().ToString();

        public async Task startClient()
        {
            try
            {
                await startSubscribing();
            }
            catch (Exception ex)
            {
                Logger.LogVerbose($"subscribe connect exception: {ex}");
                StatusChanged?.Invoke(this, $"subscribe connect exception: {ex.Message}");
            }
            try
            {
                await startPublishing();
            }
            catch (Exception ex)
            {
                Logger.LogVerbose($"publish connect exception: {ex}");
                StatusChanged?.Invoke(this, $"publish connect exception: {ex.Message}");
            }
        }

        private async Task startSubscribing()
        {
            var mqttFactory = new MqttFactory();
            this.mqttSubscribeClient = mqttFactory.CreateMqttClient();
            Action<MqttClientConnectedEventArgs> connectAction = OnSubscriberConnected;
            Action<MqttClientDisconnectedEventArgs> disonnectAction = OnSubscriberDisconnected;
            Action<MqttApplicationMessageReceivedEventArgs> receiveSubscribeAction = OnSubscriberMessageReceived;
            this.mqttSubscribeClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(connectAction);
            this.mqttSubscribeClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(disonnectAction);
            this.mqttSubscribeClient.UseApplicationMessageReceivedHandler(receiveSubscribeAction);

            await ConnectSubscriber();
        }

        private async Task startPublishing()
        {
            var mqttFactory = new MqttFactory();
            this.mqttPublishClient = mqttFactory.CreateMqttClient();
            Action<MqttApplicationMessageReceivedEventArgs> receivedHandler = HandlePublishAnswer;
            Action<MqttClientConnectedEventArgs> connectedHandler = OnPublisherConnected;
            Action<MqttClientDisconnectedEventArgs> disconnectedHandler = OnPublisherDisconnected;
            this.mqttPublishClient.UseApplicationMessageReceivedHandler(receivedHandler);
            this.mqttPublishClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(connectedHandler);
            this.mqttPublishClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(disconnectedHandler);

            await ConnectPublisher();
        }

        private async Task ConnectSubscriber()
        {
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = Guid.NewGuid().ToString(),
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = _brokerIP,
                    Port = _brokerPort,
                    TlsOptions = tlsOptions
                },
                CleanSession = true,
                //KeepAlivePeriod = TimeSpan.FromSeconds(60)
            };

            await this.mqttSubscribeClient.ConnectAsync(options);
        }

        private async Task ConnectPublisher()
        { 
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = PublishClientId,
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = _brokerIP,
                    Port = _brokerPort,
                    TlsOptions = tlsOptions
                },
                CleanSession = true,
                KeepAlivePeriod = TimeSpan.FromSeconds(5),
            };

            //options.Credentials = new MqttClientCredentials
            //{
            //    Username = "username",
            //    Password = Encoding.UTF8.GetBytes("password")
            //};

            var result = await this.mqttPublishClient.ConnectAsync(options);
            Logger.LogVerbose($"Connecting MQTT Publisher: {result.GetString()}");
        }

        private void OnSubscriberConnected(MqttClientConnectedEventArgs x)
        {
            Logger.LogVerbose($"Subscriber Connected with result {x.GetString()}");
            var resultCode = x.AuthenticateResult.ResultCode;
            if (resultCode != MqttClientConnectResultCode.Success)
            {
                StatusChanged?.Invoke(this, $"Subscriber problem: {resultCode}");
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await ConnectSubscriber();
                });
            }
            else
            {
                StatusChanged?.Invoke(this, $"Connected");
                lock (_lock)
                {
                    foreach (var topic in _subsribeTopics)
                    {
                        _ = subscribe(topic);
                    }
                }
            }
        }

        private void OnSubscriberDisconnected(MqttClientDisconnectedEventArgs x)
        {
            var resultCode = x.AuthenticateResult?.ResultCode ?? 0;
            var reason = x.AuthenticateResult?.ReasonString;
            StatusChanged?.Invoke(this, $"Disconnected: {(string.IsNullOrWhiteSpace(reason) ? "" : $" ({reason})")}");
            Logger.LogVerbose($"Subscriber Disonnected: {x.GetString()}");
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await ConnectSubscriber();
            });
        }

        private void OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs x)
        {
            var topic = x.ApplicationMessage.Topic;
            var payload = x.ApplicationMessage.ConvertPayloadToString();

            Logger.LogVerbose($"Subscriber message received: {x.GetString()}");

            MessageReceived?.Invoke(this, new MessageEventArgs() { Payload = payload, Topic = topic });
        }

        private void HandlePublishAnswer(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            Logger.LogVerbose($"Publish answer received: {eventArgs.GetString()}");
        }

        private void OnPublisherConnected(MqttClientConnectedEventArgs x)
        {
            Logger.LogVerbose($"Publisher Connected : {x.GetString()}");
        }

        private void OnPublisherDisconnected(MqttClientDisconnectedEventArgs x)
        {
            Logger.LogVerbose($"Publisher Disonnected : {x.GetString()}");
        }

        public async Task publish(string payload, string topic)
        {
            try
            {
                Logger.LogVerbose($"Publishing topic {topic}, payload {payload}");
                var utf8Payload = Encoding.UTF8.GetBytes(payload);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic.Trim())
                    .WithPayload(utf8Payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag()
                    .Build();

                if (!this.mqttPublishClient.IsConnected)
                {
                    await ConnectPublisher();
                    await Task.Run(async () =>
                    {
                        while (!this.mqttPublishClient.IsConnected)
                        {
                            await Task.Delay(500);
                        }
                    });
                }
                var result = await this.mqttPublishClient.PublishAsync(message);
            }
            catch(Exception ex)
            {
                Logger.LogVerbose($"Publishing topic {topic}, payload {payload}: {ex}");
                MessageBox.Show(ex.ToString());
            }
        }

        public async Task Subscribe(string topic)
        {
            lock (_lock)
            {
                if (!_subsribeTopics.Contains(topic))
                {
                    _subsribeTopics.Add(topic);
                }
            }

            await subscribe(topic);
        }

        private async Task subscribe(string topic)
        {
            if (this.mqttSubscribeClient.IsConnected)
            {
                await this.mqttSubscribeClient.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic.Trim()).Build());
            }
        }

        public async Task Unsubscribe(string topic)
        {
            lock (_lock)
            {
                if (_subsribeTopics.Contains(topic))
                {
                    _subsribeTopics.Remove(topic);
                }
            }
            await this.mqttSubscribeClient.UnsubscribeAsync(new string[] { topic });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        if (this.mqttSubscribeClient.IsConnected)
                        {
                            var disconnectOptions = new MqttClientDisconnectOptions()
                            {
                                ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                                ReasonString = ""
                            };
                            this.mqttSubscribeClient.DisconnectAsync(disconnectOptions, CancellationToken.None).Wait();
                        }
                        mqttSubscribeClient.Dispose();
                    }
                    catch { }
                    try
                    {
                        if (this.mqttPublishClient.IsConnected)
                        {
                            var disconnectOptions = new MqttClientDisconnectOptions()
                            {
                                ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                                ReasonString = ""
                            };
                            this.mqttPublishClient.DisconnectAsync(disconnectOptions, CancellationToken.None).Wait();
                        }
                        mqttPublishClient.Dispose();
                    }
                    catch { }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public class StatusEventArgs : EventArgs
    {
        public string Status { get; set; }

        public static implicit operator StatusEventArgs(string status)
        {
            return new StatusEventArgs() { Status = status };
        }
    }

    public delegate void StatusEventHandler(object sender, StatusEventArgs e);
}
