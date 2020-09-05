using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shutters
{
    public static class MQTTExtensions
    {
        public static string GetString(this MqttClientAuthenticateResult result)
        {
            if (result == null)
            {
                return "";
            }
            var stringBuilder = new StringBuilder($"Result code: {result.ResultCode}");
            if (!string.IsNullOrWhiteSpace(result.ReasonString))
            {
                stringBuilder.Append($", reason: {result.ReasonString}");
            }
            if (!string.IsNullOrWhiteSpace(result.ResponseInformation))
            {
                stringBuilder.Append($", response information: {result.ResponseInformation}");
            }
            if (result.UserProperties != null && result.UserProperties.Any())
            {
                stringBuilder.Append($", user properties: {string.Join(";", result.UserProperties.Select(u => $"{u.Name} = {u.Value}"))}");
            }
            return stringBuilder.ToString();
        }

        public static string GetString(this MqttClientDisconnectedEventArgs args)
        {
            if (args == null)
            {
                return "";
            }
            var stringBuilder = new StringBuilder($"Client was{(args.ClientWasConnected ? " " : " not ")} connected");
            if (args.AuthenticateResult != null)
            {
                stringBuilder.Append($", authentication result: {args.AuthenticateResult.GetString()}");
            }
            if (args.Exception != null)
            {
                stringBuilder.Append($", exception: {args.Exception}");
            }
            return stringBuilder.ToString();
        }

        public static string GetString(this MqttClientConnectedEventArgs args)
        {
            if (args == null)
            {
                return "";
            }
            if (args.AuthenticateResult != null)
            {
                return $"Authentication result: {args.AuthenticateResult.GetString()}";
            }
            return "";
        }

        public static string GetString(this MqttApplicationMessageReceivedEventArgs args)
        {
            if (args == null)
            {
                return "";
            }
            var stringBuilder = new StringBuilder($"Message from client {args.ClientId}");
            if (args.ProcessingFailed)
            {
                stringBuilder.Append($": Processing failed");
            }
            if (args.ApplicationMessage != null)
            {
                stringBuilder.Append($": {args.ApplicationMessage.GetString()}");
            }
            return stringBuilder.ToString();
        }

        public static string GetString(this MqttApplicationMessage message)
        {
            if (message == null)
            {
                return "";
            }
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message.Topic))
            {
                stringBuilder.Append($", Topic: {message.Topic}");
            }
            if (!string.IsNullOrWhiteSpace(message.ResponseTopic))
            {
                stringBuilder.Append($", ResponseTopic: {message.ResponseTopic}");
            }
            var payLoadString = message.ConvertPayloadToString();
            if (!string.IsNullOrWhiteSpace(payLoadString))
            {
                stringBuilder.Append($", Payload: {payLoadString}");
            }
            stringBuilder.Append($", QualityOfServiceLevel: {message.QualityOfServiceLevel}");
            return stringBuilder.ToString();
        }
    }
}
