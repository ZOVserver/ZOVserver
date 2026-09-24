using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ZOVserver.Services.Global.GameGlobalEventsService.Services;
using ZOVserver.Shared.Contracts.Proto;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;

namespace ZOVserver.Services.Global.GameGlobalEventsService.Grpc;

public class GameGlobalEventsGrpcService : Shared.Contracts.Proto.GameGlobalEventsService.GameGlobalEventsServiceBase
{
    public static GameEventSlotGlobalEventPublisherService? EventService { get; set; }
    public static TrophySeasonDataGlobalEventPublisherService? TrophySeasonService { get; set; }

    public override Task<TestResponse> TestR(TestRequest request, ServerCallContext context)
    {
        return Task.FromResult(new TestResponse());
    }

    public override Task<Empty> SetDoubleTokensEvent(SetDoubleTokensEventAct request, ServerCallContext context)
    {
        if (EventService is null) return Task.FromResult(new Empty());
        EventService.DoubleTokensEvent = request.Value;
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> SetShopClosed(SetShopClosedAct request, ServerCallContext context)
    {
        if (EventService is null) return Task.FromResult(new Empty());
        EventService.ShopClosed = request.Value;
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> SetBoxesClosed(SetBoxesClosedAct request, ServerCallContext context)
    {
        if (EventService is null) return Task.FromResult(new Empty());
        EventService.BoxesClosed = request.Value;
        return Task.FromResult(new Empty());
    }

    public override Task<Empty> ResetTrophySeason(Empty request, ServerCallContext context)
    {
        TrophySeasonService?.ResetTrophySeason();
        return Task.FromResult(new Empty());
    }

    public override Task<AddNewUpcomingEventResponse> AddNewUpcomingEvent(AddNewUpcomingEventRequest request,
        ServerCallContext context)
    {
        if (EventService is null)
            return Task.FromResult(new AddNewUpcomingEventResponse { Result = -10054 });

        var res = EventService.AddNewUpcomingEvent(
            request.UpcomingEvent.Slot,
            request.UpcomingEvent.Location,
            request.UpcomingEvent.Modifiers.ToList(),
            request.UpcomingEvent.MiniBoxTokensReward,
            request.UpcomingEvent.StartTime, request.UpcomingEvent.LifetimeSeconds);

        return Task.FromResult(new AddNewUpcomingEventResponse { Result = res });
    }

    public override Task<RemoveUpcomingEventResponse> RemoveUpcomingEvent(RemoveUpcomingEventRequest request,
        ServerCallContext context)
    {
        if (EventService is null)
            return Task.FromResult(new RemoveUpcomingEventResponse { Result = -10055 });

        var res = EventService.RemoveUpcomingEvent(request.EventId);

        return Task.FromResult(new RemoveUpcomingEventResponse { Result = res });
    }

    public override Task<SetLobbyThemeResponse> SetLobbyTheme(SetLobbyThemeRequest request, ServerCallContext context)
    {
        if (EventService is null || LogicDataTables.GetDataById(request.LobbyThemeGlobalId) == null)
            return Task.FromResult(new SetLobbyThemeResponse { Result = -1 });

        EventService.ThemeGlobalId = request.LobbyThemeGlobalId;

        return Task.FromResult(new SetLobbyThemeResponse { Result = 0 });
    }

    public override Task<StartMaintenanceResponse> StartMaintenance(StartMaintenanceRequest request,
        ServerCallContext context)
    {
        if (EventService is null)
            return Task.FromResult(new StartMaintenanceResponse { Result = -1 });

        EventService.MaintenanceSecondsLeft = request.SecondsLeft;

        return Task.FromResult(new StartMaintenanceResponse { Result = 0 });
    }

    public override Task<StopMaintenanceResponse> StopMaintenance(StopMaintenanceRequest request,
        ServerCallContext context)
    {
        if (EventService is null)
            return Task.FromResult(new StopMaintenanceResponse { Result = -1 });

        EventService.MaintenanceSecondsLeft = -1;

        return Task.FromResult(new StopMaintenanceResponse { Result = 0 });
    }
}