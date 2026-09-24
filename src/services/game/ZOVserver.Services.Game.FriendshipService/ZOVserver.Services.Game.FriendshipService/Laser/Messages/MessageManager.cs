using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using NLog;
using ZOVserver.Services.Game.FriendshipService.Grains;
using ZOVserver.Services.Game.FriendshipService.States;
using ZOVserver.Shared.Abstractions;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.DebugInfo;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Services.Game.FriendshipService.Laser.Messages;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
internal class MessageManager(
    FriendshipGrain grain,
    FriendshipState state,
    IPlayerSessionServiceGrain playerSession,
    IGrainFactory grainFactory) : IMessageManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly FrozenDictionary<int, MessageProcessorDelegate> Processors;
    private static readonly IReadOnlyList<(int messageType, string messageName)> ImplMessages;

    static MessageManager()
    {
        var methods = typeof(MessageManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var cd = new Dictionary<int, MessageProcessorDelegate>();
        var debugList = new List<(int, string)>();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length < 1 || !parameters[0].ParameterType.IsSubclassOf(typeof(PiranhaMessage))) continue;

            var message = parameters[0].ParameterType;
            var instance = (PiranhaMessage)Activator.CreateInstance(message)!;
            var msgId = instance.GetMessageType();

            cd.Add(msgId, CompileProcessor(method, message));

            var debugName = DebugInfoCollector.PacketCollectorY.GetValueOrDefault(msgId, ("UNKNOWN-NAME", -1)).Item1;
            debugList.Add((msgId, debugName));
        }

        Processors = cd.ToFrozenDictionary();
        ImplMessages = debugList.ToImmutableList();
    }

    public static IReadOnlyList<(int messageType, string messageName)> GetAllImplementedMessages()
    {
        return ImplMessages;
    }

    public async Task<int> ReceiveMessageAsync(PiranhaMessage piranhaMessage)
    {
        if (state.GameState == 0) return -5999;
        if (!Processors.TryGetValue(piranhaMessage.GetMessageType(), out var processor)) return -6000;

        try
        {
            return await processor(this, piranhaMessage);
        }
        catch (Exception e)
        {
            var error = e.ToString();
            Logger.Error(error);

            return await SendMessagesAndDisconnectAsync(new LoginFailedMessage
                { Capacity = 300, ErrorCode = 1, Reason = error })
                ? 0
                : -6003;
        }
    }

    public async Task<bool> SendMessagesAsync(params PiranhaMessage[] piranhaMessage)
    {
        if (state.GameState == 0 || state.SessionId == Guid.Empty)
            return false;

        var msgs = new List<PiranhaMessageStruct>();
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var message in piranhaMessage)
            {
                var messageStruct = LaserContractSerializer.SerializeToStruct(message);

                msgs.Add(messageStruct);
            }
        }

        await grain.SendMessagesToPlayerAsync(msgs.ToArray());
        return true;
    }

    public async Task<bool> SendMessagesAndDisconnectAsync(params PiranhaMessage[] piranhaMessage)
    {
        if (state.GameState == 0 || state.SessionId == Guid.Empty)
            return false;

        var msgs = new List<PiranhaMessageStruct>();
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var message in piranhaMessage)
            {
                var messageStruct = LaserContractSerializer.SerializeToStruct(message);

                msgs.Add(messageStruct);
            }
        }

        await playerSession.DisconnectPlayerWithMessagesAsync(msgs.ToArray());
        return true;
    }

    public async Task TickAsync()
    {
    }


    public async ValueTask GoodbyeAsync()
    {
    }

    private static MessageProcessorDelegate CompileProcessor(MethodInfo method, Type messageType)
    {
        var managerParam = Expression.Parameter(typeof(MessageManager), "manager");
        var messageParam = Expression.Parameter(typeof(PiranhaMessage), "message");

        var castedMessage = Expression.Convert(messageParam, messageType);
        var methodCall = Expression.Call(managerParam, method, castedMessage);

        if (method.ReturnType == typeof(Task<int>))
            return Expression.Lambda<MessageProcessorDelegate>(methodCall, managerParam, messageParam).Compile();

        if (method.ReturnType == typeof(Task<bool>))
        {
            var bridgeMethod = typeof(MessageManager)
                .GetMethod(nameof(ConvertTaskBoolToTaskInt), BindingFlags.Static | BindingFlags.NonPublic)!;

            var wrappedCall = Expression.Call(bridgeMethod, methodCall);

            return Expression.Lambda<MessageProcessorDelegate>(wrappedCall, managerParam, messageParam).Compile();
        }

        Expression body = method.ReturnType switch
        {
            var t when t == typeof(int) =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], methodCall),
            var t when t == typeof(bool) =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)],
                    Expression.Condition(methodCall, Expression.Constant(0), Expression.Constant(-1))),
            _ =>
                Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], Expression.Constant(-6001))
        };

        return Expression.Lambda<MessageProcessorDelegate>(body, managerParam, messageParam).Compile();
    }

    private static async Task<int> ConvertTaskBoolToTaskInt(Task<bool> task)
    {
        return await task ? 0 : -1;
    }

    private Task<bool> SetBlockFriendRequestsMessageReceived(SetBlockFriendRequestsMessage message)
    {
        state.FriendRequestsBlocked = message.State;
        return Task.FromResult(true);
    }

    private async Task<int> AskForFriendListMessageReceived(AskForFriendListMessage message)
    {
        await SendMessagesAsync(new FriendListMessage
        {
            FriendRequestsBlocked = state.FriendRequestsBlocked,
            FriendEntries = state.Friends.Select(x => x.Value).ToArray()
        });

        return 0;
    }

    private async Task<int> AskForFriendSuggestionsMessageReceived(AskForFriendSuggestionsMessage message)
    {
        await SendMessagesAsync(new FriendSuggestionsMessage
        {
            FriendSuggestionEntries = state.Suggestions.Select(x => x.Value).ToArray()
        });

        return 0;
    }

    private async Task<bool> AddFriendMessageReceived(AddFriendMessage message)
    {
        var res = await grain.CreateFriendRequestAsync(message.AccountId, message.Reason);

        return res switch
        {
            -1 or -2 or -8 or -9 or -14 or -15 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 0 }),
            -3 or -10 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 4 }),
            -4 or -5 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 3 }),
            -6 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 7 }),
            -7 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 1 }),
            -11 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 2 }),
            -12 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 8 }),
            -13 => await SendMessagesAsync(new AddFriendFailedMessage { ErrorCode = 2 }), // requests blocked
            _ => true
        };
    }

    private async Task<bool> AcceptFriendMessageReceived(AcceptFriendMessage message)
    {
        var res = await grain.AcceptFriendRequestAsync(message.AccountId);

        return res switch
        {
            -1 or -2 or -3 or -5 or -6 or -8 =>
                await SendMessagesAsync(new AcceptFriendFailedMessage { ErrorCode = 0 }),
            -4 => await SendMessagesAsync(new AcceptFriendFailedMessage { ErrorCode = 3 }),
            -7 => await SendMessagesAsync(new AcceptFriendFailedMessage { ErrorCode = 2 }),
            _ => true
        };
    }

    private async Task<bool> RemoveFriendMessageReceived(RemoveFriendMessage message)
    {
        var res = await grain.RemoveMyFriendAsync(message.AccountId);
        return res == 0;
    }

    private delegate Task<int> MessageProcessorDelegate(MessageManager manager, PiranhaMessage message);
}