namespace MTDS.Api.Infrastructure.Communication;


public interface ITransportCommunicator
{
    Task<bool> SendCommandToTransportAsync(int transportId, string actionType, CancellationToken cancel);
}

public class MockTransportCommunicator : ITransportCommunicator
{
    public async Task<bool> SendCommandToTransportAsync(int transportId, string actionType, CancellationToken cancel)
    {
        await Task.Delay(500, cancel);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[NETWORK] Физический сигнал '{actionType}', успешно доставлен на бортовой компьютер автобуса  {transportId}");
        Console.ResetColor();
        return true;
    }
}