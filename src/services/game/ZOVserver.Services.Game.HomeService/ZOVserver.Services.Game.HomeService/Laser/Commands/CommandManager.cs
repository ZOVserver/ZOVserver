using NLog;
using ZOVserver.Services.Game.HomeService.Laser.Commands.Executors;
using ZOVserver.Services.Game.HomeService.Laser.Mode;
using ZOVserver.Services.Game.HomeService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Commands;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;

namespace ZOVserver.Services.Game.HomeService.Laser.Commands;

public class CommandManager(
    IHomeServiceGrain grain,
    HomeState state,
    IMessageManager messageManager,
    IGrainFactory grainFactory)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private ClientCommandsExecutor? _clientCommandsExecutor;
    private int _lastCommandId;
    private ServerCommandsExecutor? _serverCommandsExecutor;

    public LogicHomeMode? HomeMode { get; set; }

    public void InitExecutors()
    {
        if (HomeMode == null) return;

        _clientCommandsExecutor =
            new ClientCommandsExecutor(grain, state, HomeMode, this, messageManager, grainFactory);
        _serverCommandsExecutor =
            new ServerCommandsExecutor(grain, state, HomeMode, this, messageManager, grainFactory);
    }

    public async Task<int> ReceiveCommandAsync(LogicCommand? logicCommand)
    {
        if (_clientCommandsExecutor == null) return -997;
        if (logicCommand == null) return -999;

        var id = logicCommand.GetCommandType();

        if (id < 500)
        {
            Logger.Info($"Detected server command {id}!"); // venom
            return 0;
        }

        Logger.Info($"Received client command {id}.");
        return await _clientCommandsExecutor.ExecuteCommandAsync(logicCommand);
    }

    public async Task<int> SendCommandsAsync(params LogicServerCommand[] commands)
    {
        if (_serverCommandsExecutor == null) return -998;

        var messages = new List<PiranhaMessage>();
        {
            foreach (var command in commands)
            {
                command.Id = Interlocked.Increment(ref _lastCommandId);

                var message = new AvailableServerCommandMessage { Capacity = 512 };
                message.SetServerCommand(command);

                messages.Add(message);

                Logger.Info($"Sending server command {command.GetCommandType()}.");
            }
        }

        foreach (var command in commands)
        {
            var rex = await _serverCommandsExecutor.ExecuteCommandAsync(command);
            if (rex >= 0) continue;

            Logger.Error($"Failed to execute server command {command.GetCommandType()}. Error code: {rex}");
            var r = await messageManager.SendMessagesAndDisconnectAsync(new OutOfSyncMessage
                { Capacity = 32 });
            return r ? 0 : -1;
        }

        return await messageManager.SendMessagesAsync(messages.ToArray()) ? 0 : -996;
    }

    public async Task TickAsync()
    {
    }

    public ValueTask GoodbyeAsync()
    {
        _clientCommandsExecutor = null;
        _serverCommandsExecutor = null;

        HomeMode = null;

        return ValueTask.CompletedTask;
    }
}