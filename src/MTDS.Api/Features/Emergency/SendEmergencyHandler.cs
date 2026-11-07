using MediatR;
using MTDS.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Communication;
using MTDS.Api.Features.Audit;

namespace MTDS.Api.Features.Emergency;

public class SendEmergencyHandler : IRequestHandler<SendEmergencyCommand, bool>
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;
    private readonly ITransportCommunicator _communicator;

    public SendEmergencyHandler(AppDbContext db, IMediator mediator, ITransportCommunicator communicator)
    {
        _db = db;
        _mediator = mediator;
        _communicator = communicator;
    }

    public async Task<bool> Handle(SendEmergencyCommand command, CancellationToken cancel)
    {
        var transport = await _db.Transports.FindAsync(new object[] { command.TransportId }, cancel);


        bool isDelivered = await _communicator.SendCommandToTransportAsync(command.TransportId, command.ActionType, cancel);
        if (!isDelivered)
            return false;
        if (transport == null)
            return false;

        switch (command.ActionType)
        {
            case "STOP":
                {
                    transport.Status = "EMERGENCY_STOP";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Транспортному средству {command.TransportId} отправлена команда СТОП!");
                    Console.ResetColor();
                    break;
                }
            case "RETURN_TO_PARK":
                {
                    transport.Status = "RETURNING_TO_PARK";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Транспортному средству {command.TransportId} отправлена команда ВЕРНУТЬСЯ В ПАРК!");
                    Console.ResetColor();
                    break;
                }
            case "RADIO_CALL_REQUIRED":
                {
                    transport.Status = "RADIO_CALL_REQUIRED";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Водителю отправлена команда ВЫЙТИ НА СВЯЗЬ ПО РАЦИИ!");
                    Console.ResetColor();
                    break;
                }
            case "ROUTE_DETOUR":
                {
                    transport.Status = "ROUTE_DOTOUR";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Транспортному средству {command.TransportId} отправлена команда ОБЪЕЗД!");
                    Console.ResetColor();
                    break;
                }
            case "DOORS_UNLOCK":
                {
                    transport.Status = "DOORS_UNLOCK";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Транспортному средству {command.TransportId} отправлена команда ПРИНУДИТЕЛЬНАЯ РАЗБЛОКИРОВКА ДВЕРЕЙ!");
                    Console.ResetColor();
                    break;
                }
            case "SILENT_ALARM_ACK":
                {
                    transport.Status = "SILENT_ALARM_ACK";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[EMERGENCY CALL] Транспортному средство {command.TransportId} подтвердило команду ТИХАЯ ТРЕВОГА!");
                    Console.ResetColor();
                    break;
                }
        }



        await _db.SaveChangesAsync(cancel);
        await _mediator.Publish(new TransportEmergencyCommandSentEvent
        {
            TransportId = command.TransportId,
            DispatcherId = command.DispatcherId,
            ActionType = command.ActionType,
            Reason = command.Reason
        }, cancel);

        return true;
    }

}