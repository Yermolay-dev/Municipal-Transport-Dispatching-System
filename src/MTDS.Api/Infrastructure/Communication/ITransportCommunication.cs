using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
namespace MTDS.Api.Infrastructure.Communication;

public interface ITransportCommunicator
{
    Task<bool> SendCommandToTransportAsync(int transportId, string actionType, CancellationToken cancel);
}

public class MqttTransportCommunicator : ITransportCommunicator
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;

    public MqttTransportCommunicator()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        _options = new MqttClientOptionsBuilder().WithTcpServer("localhost", 1883).WithClientId("MtdsBackend_Commander").Build();
    }

    public async Task<bool> SendCommandToTransportAsync(int transportId, string actionType, CancellationToken cancel)
    {
        try
        {
            if (!_mqttClient.IsConnected)
                await _mqttClient.ConnectAsync(_options, cancel);
            string topic = $"commands/bus/{transportId}";
            var payload = new
            {
                command = actionType,
                timestamp = DateTime.UtcNow
            };
            string jsonPayLoad = JsonSerializer.Serialize(payload);
            var mqttMessage = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(jsonPayLoad).WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Build();
            await _mqttClient.PublishAsync(mqttMessage, cancel);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[NETWORK] Физический сигнал '{actionType}', успешно доставлен на бортовой компьютер автобуса  {transportId}");
            Console.ResetColor();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[MQTT ERROR] Не удалось отправить команду: {ex.Message}");
            Console.ResetColor();
            return false;
        }

    }
}