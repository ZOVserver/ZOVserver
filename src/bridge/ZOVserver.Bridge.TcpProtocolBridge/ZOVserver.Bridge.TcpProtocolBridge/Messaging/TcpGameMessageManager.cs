using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NLog;
using ZOVserver.Bridge.TcpProtocolBridge.Abstractions;
using ZOVserver.Bridge.TcpProtocolBridge.Manager;
using ZOVserver.Bridge.TcpProtocolBridge.Sessions;
using ZOVserver.RasputinProtect;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Client;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.Localization;
using ZOVserver.Shared.TitanRemnants.Mathem.Shared;
using ZOVserver.Shared.TitanRemnants.PAssets;

namespace ZOVserver.Bridge.TcpProtocolBridge.Messaging;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public class TcpGameMessageManager(LaserHighLevelTcpSession session) : ITcpGameMessageManager, IBridgeObserver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly FrozenDictionary<int, MessageProcessorDelegate> Processors;

    private static readonly Task<int> TaskSuccess = Task.FromResult(0);
    private static readonly Task<int> TaskFailure = Task.FromResult(-1);
    private static readonly Task<int> TaskError6001 = Task.FromResult(-6001);
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private byte _b;

    private bool _closed;
    private bool _isAccountSessionSwitched;
    private bool _isObserverRegistered;
    private bool _keepAliveGrainsException;
    private DateTime _keepAliveLastRecvTime = DateTime.UtcNow;

    private IBridgeObserver? _selfReference;

    static TcpGameMessageManager()
    {
        var methods = typeof(TcpGameMessageManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var cd = new Dictionary<int, MessageProcessorDelegate>();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length < 1 || !parameters[0].ParameterType.IsSubclassOf(typeof(PiranhaMessage))) continue;

            var message = parameters[0].ParameterType;

            var instance = (PiranhaMessage)Activator.CreateInstance(message)!;
            var msgId = instance.GetMessageType();

            cd.Add(msgId, CompileProcessor(method, message));
        }

        Processors = cd.ToFrozenDictionary();
    }

    private IPlayerSessionServiceGrain? PlayerSession { get; set; }
    private IHomeServiceGrain? Home { get; set; }
    private IFriendshipServiceGrain? Friendship { get; set; }

    private ReadOnlyDictionary<int, (string, HashSet<string>)>? ImplementedMessages { get; set; }

    public async Task SendBridgeEventAsync(BridgeEvent bridgeEvent)
    {
        await ReceiveBridgeEvent(bridgeEvent);
    }

    public Task<int> ReceiveMessageAsync(PiranhaMessage piranhaMessage)
    {
        if (!Processors.TryGetValue(piranhaMessage.GetMessageType(), out var processor))
            return Task.FromResult(-6000);

        return processor(this, piranhaMessage);
    }

    public async Task TickAsync()
    {
        if (_closed) return;

        if (!await _semaphore.WaitAsync(5))
        {
            Logger.Warn($"Tick for session {session.SessionId} is already running. Skipping...");
            return;
        }

        try
        {
            // tick code

            if (EventsManager.GetMaintenanceSecondsLeft() > 0)
                await SendMessageAsync(new LoginFailedMessage
                {
                    Capacity = 300,
                    ErrorCode = 10,
                    SecondsUntilMaintenanceEnd = EventsManager.GetMaintenanceSecondsLeft(),
                    MaintenanceType = 1
                });

            if ((DateTime.UtcNow - _keepAliveLastRecvTime).TotalSeconds > 30)
                await session.LowLevelSession.DisconnectAsync();
        }
        catch (Exception e)
        {
            Logger.Error($"Error while 'Tick' procession! {e}.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask GoodbyeAsync()
    {
        if (_closed) return;

        try
        {
            if (!_keepAliveGrainsException)
            {
                await OnUnregisterGrains();
                await OnDisconnectGrains();
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Error while 'Goodbye' procession! {e}.");
        }

        _closed = true;
    }

    public async Task<int> ReceiveMaybeImplementedMessagesByGrains(List<PiranhaMessage> piranhaMessage)
    {
        if (ImplementedMessages == null)
            return -1;

        var playerSessionMessages = new List<PiranhaMessageStruct>();
        var homeMessages = new List<PiranhaMessageStruct>();
        var friendshipMessages = new List<PiranhaMessageStruct>();

        foreach (var message in piranhaMessage)
        {
            if (!ImplementedMessages.TryGetValue(message.GetMessageType(), out var mesg)) continue;

            foreach (var grain in mesg.Item2)
                switch (grain)
                {
                    case "player_session":
                        playerSessionMessages.Add(new PiranhaMessageStruct
                        {
                            MessageType = message.GetMessageType(), MessageName = mesg.Item1,
                            MessagePayload = message.Payload!
                        });
                        break;
                    case "home":
                        homeMessages.Add(new PiranhaMessageStruct
                        {
                            MessageType = message.GetMessageType(), MessageName = mesg.Item1,
                            MessagePayload = message.Payload!
                        });
                        break;
                    case "friendship":
                        friendshipMessages.Add(new PiranhaMessageStruct
                        {
                            MessageType = message.GetMessageType(), MessageName = mesg.Item1,
                            MessagePayload = message.Payload!
                        });
                        break;
                }
        }

        try
        {
            if (PlayerSession != null && playerSessionMessages.Count > 0)
            {
                var res = await PlayerSession.ReceiveImplementedMessagesAsync(playerSessionMessages.ToArray());

                if (!res)
                    return -2;
            }

            if (Home != null && homeMessages.Count > 0)
            {
                var res = await Home.ReceiveImplementedMessagesAsync(homeMessages.ToArray());

                if (!res)
                    return -2;
            }

            if (Friendship != null && friendshipMessages.Count > 0)
            {
                var res = await Friendship.ReceiveImplementedMessagesAsync(friendshipMessages.ToArray());

                if (!res)
                    return -2;
            }
        }
        catch (Exception e)
        {
            var error = e.ToString();

            Logger.Error(error);

            return await SendMessageAsync(new LoginFailedMessage
            {
                Capacity = 300,
                ErrorCode = 1,
                Reason = error
            });
        }

        return 0;
    }

    private static MessageProcessorDelegate CompileProcessor(MethodInfo method, Type messageType)
    {
        var managerParam = Expression.Parameter(typeof(TcpGameMessageManager), "manager");
        var messageParam = Expression.Parameter(typeof(PiranhaMessage), "message");

        var castedMessage = Expression.Convert(messageParam, messageType);
        var methodCall = Expression.Call(managerParam, method, castedMessage);

        if (method.ReturnType == typeof(Task<int>))
            return Expression.Lambda<MessageProcessorDelegate>(methodCall, managerParam, messageParam).Compile();

        if (method.ReturnType == typeof(Task<bool>))
        {
            var bridgeMethod = typeof(TcpGameMessageManager).GetMethod(nameof(ConvertTaskBoolToTaskInt),
                BindingFlags.Static | BindingFlags.NonPublic)!;
            var wrappedCall = Expression.Call(bridgeMethod, methodCall);
            return Expression.Lambda<MessageProcessorDelegate>(wrappedCall, managerParam, messageParam).Compile();
        }

        Expression body = method.ReturnType switch
        {
            var t when t == typeof(int) =>
                Expression.Condition(
                    Expression.Equal(methodCall, Expression.Constant(0)),
                    Expression.Constant(TaskSuccess),
                    Expression.Condition(
                        Expression.Equal(methodCall, Expression.Constant(-1)),
                        Expression.Constant(TaskFailure),
                        Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(int)], methodCall)
                    )
                ),

            var t when t == typeof(bool) =>
                Expression.Condition(methodCall, Expression.Constant(TaskSuccess), Expression.Constant(TaskFailure)),

            _ => Expression.Constant(TaskError6001)
        };

        return Expression.Lambda<MessageProcessorDelegate>(body, managerParam, messageParam).Compile();
    }

    private static Task<int> ConvertTaskBoolToTaskInt(Task<bool> task)
    {
        if (task.IsCompletedSuccessfully)
            return task.Result ? TaskSuccess : TaskFailure;

        return ExecuteAsync(task);

        static async Task<int> ExecuteAsync(Task<bool> t)
        {
            return await t ? 0 : -1;
        }
    }

    public async Task<int> SendMessageAsync(PiranhaMessage piranhaMessage)
    {
        var messaging = session.GetGameMessaging();
        if (messaging == null) return -7000;

        await messaging.SendAsync(piranhaMessage);
        return 0;
    }

    private async Task<int> ClientHelloMessageReceived(ClientHelloMessage clientHelloMessage)
    {
        if (clientHelloMessage.A1 != 2)
            return -1;

        if (clientHelloMessage.A2 != 1945)
            return -1;

        if (clientHelloMessage.A3 != Fingerprint.GetMajorVersion())
            return -1;

        if (clientHelloMessage.A5 != Fingerprint.GetBuildVersion())
            return -1;

        var chsbyte =
            GoodbyeMyLoveGoodbye.ChvR(clientHelloMessage.A7, clientHelloMessage.A8, clientHelloMessage.A6, 67);

        if (chsbyte == 0)
            return -1;

        _b = chsbyte;

        var sh = new ServerHelloMessage
        {
            Capacity = 30,
            ChsByte = chsbyte
        };

        return await SendMessageAsync(sh);
    }

    private Task<int> ClientCryptoErrorMessageReceived(ClientCryptoErrorMessage clientCryptoErrorMessage)
    {
        return Task.FromResult(-1);
    }

    private async Task<int> KeepAliveMessageReceived(KeepAliveMessage _)
    {
        try
        {
            await KeepAliveGrains();
        }
        catch (Exception e)
        {
            Logger.Error($"Error! {e}.");

            _keepAliveGrainsException = true;
            return -1;
        }

        _keepAliveLastRecvTime = DateTime.UtcNow;

        return
            await SendMessageAsync(new KeepAliveServerMessage { Capacity = 0 });
    }

    private static long GenerateAccountId()
    {
        using var rng = RandomNumberGenerator.Create();

        var high = RandomNumberGenerator.GetInt32(0, byte.MaxValue);
        var low = RandomNumberGenerator.GetInt32(0, int.MaxValue);

        return new LogicLong(high, low);
    }

    private static string GeneratePassToken()
    {
        var segment1 = new string(new[]
        {
            (char)('A' + GetSecureRandomInt(0, 25)),
            (char)('A' + GetSecureRandomInt(0, 25)),
            (char)('A' + GetSecureRandomInt(0, 25))
        });

        var segment2 = GetSecureRandomInt(10_000_000, 99_999_999).ToString();

        var segment3 = new char[10];
        {
            for (var i = 0; i < 10; i++)
            {
                var c = (char)('a' + GetSecureRandomInt(0, 25));
                segment3[i] = i % 2 == 0 ? char.ToUpper(c) : c;
            }
        }

        var segment4 = new[] { '3', '5', '7' }[GetSecureRandomInt(0, 2)];

        return $"{segment1}_{segment2}_{new string(segment3)}_{segment4}";
    }

    private static int GetSecureRandomInt(int min, int max)
    {
        if (min > max) throw new ArgumentException("Min must not be greater than max");

        var randomBytes = RandomNumberGenerator.GetBytes(4);

        var normalized = BitConverter.ToUInt32(randomBytes) / (uint.MaxValue + 1.0);

        return (int)(min + (max - min + 1) * normalized);
    }

    private async Task<int> LoginMessageReceived(LoginMessage loginMessage)
    {
        if (!loginMessage.IsAndroid)
            return await SendMessageAsync(new LoginFailedMessage
            {
                Capacity = 300,
                ErrorCode = 1,
                Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 0)
            });

        var invalidClient = !loginMessage.ResourceSha.Equals(Fingerprint.GetResourceSha()) ||
                            !loginMessage.ClientMajor.Equals(Fingerprint.GetMajorVersion()) ||
                            !loginMessage.ClientBuild.Equals(Fingerprint.GetBuildVersion()) ||
                            !loginMessage.AppVersion.Equals(
                                $"{Fingerprint.GetMajorVersion()}.{Fingerprint.GetBuildVersion()}");

        if (invalidClient)
            return await SendMessageAsync(new LoginFailedMessage
            {
                Capacity = 300,
                ErrorCode = 1,
                Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 2),
                UpdateUrl = "https://t.me/ZOVserver"
            });

        if (EventsManager.GetMaintenanceSecondsLeft() > 0)
            return await SendMessageAsync(new LoginFailedMessage
            {
                Capacity = 300,
                ErrorCode = 10,
                SecondsUntilMaintenanceEnd = EventsManager.GetMaintenanceSecondsLeft(),
                MaintenanceType = 1
            });

        try
        {
            if (loginMessage.AccountId <= 0)
            {
                if (!string.IsNullOrWhiteSpace(loginMessage.PassToken))
                    return await SendMessageAsync(new LoginFailedMessage
                    {
                        Capacity = 300,
                        ErrorCode = 1,
                        Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 4)
                    });

                try
                {
                    loginMessage.AccountId = GenerateAccountId();
                    loginMessage.PassToken = GeneratePassToken();
                    loginMessage.CreatedInServer = true;

                    PlayerSession = ClientHelper.GetPlayerSession(loginMessage.AccountId);

                    if (!await PlayerSession.CreatePassToken(loginMessage.PassToken))
                        return await SendMessageAsync(new LoginFailedMessage
                        {
                            Capacity = 300,
                            ErrorCode = 1,
                            Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 3)
                        });
                }
                catch (Exception e)
                {
                    Logger.Error(e);

                    return await SendMessageAsync(new LoginFailedMessage
                    {
                        Capacity = 300,
                        ErrorCode = 1,
                        Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 3) +
                                 $" => 1_{e.Message}"
                    });
                }
            }
            else
            {
                try
                {
                    PlayerSession = ClientHelper.GetPlayerSession(loginMessage.AccountId);

                    if (await PlayerSession.GetPlayTimeSeconds() <= 0)
                        return await SendMessageAsync(new LoginFailedMessage
                        {
                            Capacity = 300,
                            ErrorCode = 1,
                            Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 5)
                        });

                    if (!await PlayerSession.VerifyPassToken(loginMessage.PassToken))
                        return await SendMessageAsync(new LoginFailedMessage
                        {
                            Capacity = 300,
                            ErrorCode = 1,
                            Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 6)
                        });
                }
                catch (Exception e)
                {
                    Logger.Error(e);

                    return await SendMessageAsync(new LoginFailedMessage
                    {
                        Capacity = 300,
                        ErrorCode = 1,
                        Reason = LocalizationCache.GetTranslation(loginMessage.PreferredLanguage, 3) +
                                 $" => 2_{e.Message}"
                    });
                }
            }

            if (PlayerSession == null)
                return -1;

            Home = ClientHelper.GetHomeGrain(loginMessage.AccountId);

            Friendship = ClientHelper.GetFriendshipGrain(loginMessage.AccountId);

            if (loginMessage.CreatedInServer)
                await BuildNewAccountGrains(loginMessage.AccountId);
            else
                await BuildIfNeededAccountGrains(loginMessage.AccountId);

            var resend = true;
            var attempt = 0;
            while (resend)
            {
                var loginRstate = await PlayerSession.ReceiveLoginMessageAsync(new LoginMessageStruct
                {
                    AccountId = loginMessage.AccountId,
                    PassToken = loginMessage.PassToken,

                    ClientMajor = loginMessage.ClientMajor,
                    ClientMinor = loginMessage.ClientMinor,
                    ClientBuild = loginMessage.ClientBuild,
                    ResourceSha = loginMessage.ResourceSha,

                    Device = loginMessage.Device,
                    PreferredLanguage = loginMessage.PreferredLanguage,
                    PreferredDeviceLanguage = loginMessage.PreferredDeviceLanguage,

                    OsVersion = loginMessage.OsVersion,
                    IsAndroid = loginMessage.IsAndroid,
                    AndroidId = loginMessage.AndroidId
                }, loginMessage.CreatedInServer, attempt > 0);

                resend = loginRstate.Item1;

                if (attempt++ > 5)
                    return -1;

                await Task.Delay(100);
                if (!loginRstate.Item2) continue;

                await session.LowLevelSession.DisconnectAsync();
                return 0;
            }

            await Task.Delay(33);

            await OnConnectGrains();

            _selfReference = ClientHelper.Client.CreateObjectReference<IBridgeObserver>(this);
            await OnRegisterGrains();

            await Task.Delay(33);

            await GetAndSaveAllImplementedMessagesGrains();

            await Task.Delay(33);

            var loginRes = await PlayerSession.LoginOkAsync();

            if (!loginRes)
            {
                _keepAliveLastRecvTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);

                Home = null;
                Friendship = null;
                return 0;
            }

            await Home.SetB(_b);
            _b = 0;

            await Home.SendOwnHomeDataAsync();
            await Home.SendMyAllianceAsync();
        }
        catch (Exception e)
        {
            var ex = e.ToString();

            Logger.Error(ex);

            return await SendMessageAsync(new LoginFailedMessage
            {
                Capacity = 300,
                ErrorCode = 1,
                Reason = ex
            });
        }

        return 0;
    }

    private async Task ReceiveBridgeEvent(BridgeEvent bridgeEvent)
    {
        if (bridgeEvent.SessionId != session.SessionId)
        {
            if (PlayerSession != null)
            {
                var lang = await PlayerSession.GetPlayerLocalizationGlobalId();

                await SendMessageAsync(new LoginFailedMessage
                {
                    Capacity = 300,
                    ErrorCode = 1,
                    Reason = LocalizationCache.GetTranslation(lang, 7) + $" {bridgeEvent.EventType}"
                });
            }
            else
            {
                await session.LowLevelSession.DisconnectAsync();
            }

            return;
        }

        switch (bridgeEvent.EventType)
        {
            case 10:
            {
                await session.LowLevelSession.DisconnectAsync();
                break;
            }
            case 20:
            {
                var messaging = session.GetGameMessaging();
                if (messaging == null) return;

                if (bridgeEvent.PiranhaMessages == null) return;

                foreach (var message in bridgeEvent.PiranhaMessages)
                {
                    var res = await messaging.EncryptAndWriteAsync(message.MessageType, message.MessageVersion,
                        message.MessagePayload);

                    if (res == 0) continue;
                    await session.LowLevelSession.DisconnectAsync();
                    return;
                }

                break;
            }
            case 30:
            {
                var messaging = session.GetGameMessaging();
                if (messaging == null) return;

                if (bridgeEvent.PiranhaMessages != null)
                    foreach (var message in bridgeEvent.PiranhaMessages)
                    {
                        var res = await messaging.EncryptAndWriteAsync(message.MessageType,
                            message.MessageVersion,
                            message.MessagePayload);

                        if (res == 0) continue;
                        await session.LowLevelSession.DisconnectAsync();
                        return;
                    }

                await Task.Delay(1000);
                await session.LowLevelSession.DisconnectAsync();
                break;
            }
            case 40:
            {
                var messaging = session.GetGameMessaging();
                if (messaging == null) return;

                _isAccountSessionSwitched = true;

                if (bridgeEvent.PiranhaMessages != null)
                    foreach (var message in bridgeEvent.PiranhaMessages)
                    {
                        var res = await messaging.EncryptAndWriteAsync(message.MessageType,
                            message.MessageVersion,
                            message.MessagePayload);

                        if (res == 0) continue;
                        await session.LowLevelSession.DisconnectAsync();
                        return;
                    }

                await Task.Delay(1000);
                await session.LowLevelSession.DisconnectAsync();
                break;
            }
        }
    }

    private async Task BuildNewAccountGrains(long accountId)
    {
        if (PlayerSession != null)
            await PlayerSession.BuildNewAccount(accountId);

        if (Home != null)
            await Home.BuildNewAccount(accountId);

        if (Friendship != null)
            await Friendship.BuildNewAccount(accountId);

        // TODO: BuildNewAccount from other grains
    }

    private async Task BuildIfNeededAccountGrains(long accountId)
    {
        if (PlayerSession != null && await PlayerSession.GetAccountCreatedTime() == default)
            await PlayerSession.BuildNewAccount(accountId);

        if (Home != null && await Home.GetHomeCreatedTime() == default)
            await Home.BuildNewAccount(accountId);

        if (Friendship != null && await Friendship.GetFriendshipCreatedTime() == default)
            await Friendship.BuildNewAccount(accountId);

        // TODO: BuildNewAccount from other grains
    }

    private async Task OnConnectGrains()
    {
        var serverIp = session.LowLevelSession.ServerIp;
        var serverPort = session.LowLevelSession.ServerPort;
        var clientIp = session.LowLevelSession.ClientIp;
        var clientPort = session.LowLevelSession.ClientPort;
        var sessionId = session.SessionId;

        if (PlayerSession != null)
            await PlayerSession.OnConnectedAsync(serverIp, serverPort, clientIp, clientPort, sessionId);

        if (Home != null)
            await Home.OnConnectedAsync(serverIp, serverPort, clientIp, clientPort, sessionId);

        if (Friendship != null)
            await Friendship.OnConnectedAsync(serverIp, serverPort, clientIp, clientPort, sessionId);

        // TODO: OnConnectedAsync from other grains
    }

    private async Task OnRegisterGrains()
    {
        if (_isObserverRegistered) return;
        if (_selfReference == null) return;

        if (PlayerSession != null)
            await PlayerSession.RegisterBridgeObserverAsync(_selfReference);

        if (Home != null)
            await Home.RegisterBridgeObserverAsync(_selfReference);

        if (Friendship != null)
            await Friendship.RegisterBridgeObserverAsync(_selfReference);

        _isObserverRegistered = true;
    }

    private async Task OnUnregisterGrains()
    {
        if (_isAccountSessionSwitched) return;
        if (!_isObserverRegistered) return;
        if (_selfReference == null) return;

        if (PlayerSession != null)
            await PlayerSession.UnregisterBridgeObserverAsync(_selfReference);

        if (Home != null)
            await Home.UnregisterBridgeObserverAsync(_selfReference);

        if (Friendship != null)
            await Friendship.UnregisterBridgeObserverAsync(_selfReference);

        _isObserverRegistered = false;
    }

    private async Task OnDisconnectGrains()
    {
        if (_isAccountSessionSwitched) return;

        var time = DateTime.UtcNow;

        // TODO: OnDisconnectedAsync from other grains

        if (Home != null)
            await Home.OnDisconnectedAsync(time, false);
        Home = null;

        if (PlayerSession != null)
            await PlayerSession.OnDisconnectedAsync(time, false);
        PlayerSession = null;

        if (Friendship != null)
            await Friendship.OnDisconnectedAsync(time, false);
        Friendship = null;
    }

    private async Task KeepAliveGrains()
    {
        if (PlayerSession != null)
            await PlayerSession.KeepAliveAsync();

        if (Home != null)
            await Home.KeepAliveAsync();

        if (Friendship != null)
            await Friendship.KeepAliveAsync();

        // TODO: KeepAliveAsync from other grains
    }

    private async Task GetAndSaveAllImplementedMessagesGrains()
    {
        var implmsgs = new Dictionary<int, (string, HashSet<string>)>();

        if (PlayerSession != null)
        {
            var messages = await PlayerSession.GetAllImplementedMessagesAsync();

            foreach (var msg in messages)
                if (!implmsgs.TryAdd(msg.messageType, (msg.messageName, ["player_session"])))
                    implmsgs[msg.messageType].Item2.Add("player_session");
        }

        if (Home != null)
        {
            var messages = await Home.GetAllImplementedMessagesAsync();

            foreach (var msg in messages)
                if (!implmsgs.TryAdd(msg.messageType, (msg.messageName, ["home"])))
                    implmsgs[msg.messageType].Item2.Add("home");
        }

        if (Friendship != null)
        {
            var messages = await Friendship.GetAllImplementedMessagesAsync();

            foreach (var msg in messages)
                if (!implmsgs.TryAdd(msg.messageType, (msg.messageName, ["friendship"])))
                    implmsgs[msg.messageType].Item2.Add("friendship");
        }

        ImplementedMessages = implmsgs.AsReadOnly();
    }

    private delegate Task<int> MessageProcessorDelegate(TcpGameMessageManager manager, PiranhaMessage message);
}