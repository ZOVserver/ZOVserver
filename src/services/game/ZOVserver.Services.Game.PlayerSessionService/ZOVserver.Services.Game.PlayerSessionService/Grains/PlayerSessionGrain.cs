using System.Security.Cryptography;
using System.Text;
using NLog;
using Orleans.Providers;
using ZOVserver.Services.Game.PlayerSessionService.Laser.Messages;
using ZOVserver.Services.Game.PlayerSessionService.States;
using ZOVserver.Services.Game.PlayerSessionService.Telemetry;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;
using ZOVserver.Shared.TitanRemnants.PAssets;

namespace ZOVserver.Services.Game.PlayerSessionService.Grains;

[StorageProvider(ProviderName = "MongoStorage")]
// ReSharper disable once UnusedType.Global
public class PlayerSessionGrain : Grain<PlayerSessionState>, IPlayerSessionServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private IBridgeObserver? _bridgeObserver;
    private bool _isCountedInOnline;

    private MessageManager? _messageManager;
    private string? _passToken;
    private IDisposable? _tickTimer;

    public async ValueTask OnConnectedAsync(string serverIp, int serverPort, string clientIp, int clientPort,
        Guid sessionId)
    {
        if (State.HashedPassToken == null) return;

        State.GameState = 1;
        State.SessionId = sessionId;

        State.PlayTimeSeconds += 6;
        State.SessionsCount++;

        State.ConnectionsCount++;
        State.LastActiveTime = DateTimeOffset.UtcNow;

        var server = $"tcp://{serverIp}:{serverPort}_s";
        var client = $"tcp://{clientIp}_c";

        if (!State.ServersInfo.TryAdd(server, 1))
            State.ServersInfo[server]++;

        if (!State.ClientsInfo.TryAdd(client, 1))
            State.ClientsInfo[client]++;

        _tickTimer ??= this.RegisterGrainTimer<object?>(
            async _ => await TickAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            null,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(3),
                Period = TimeSpan.FromSeconds(5),
                Interleave = false
            });

        _messageManager ??= new MessageManager(this, State);

        State.LastKeepAliveReceivedTime = DateTime.UtcNow;

        if (!_isCountedInOnline)
        {
            MetricsClient.ObservePlayerConnected();
            _isCountedInOnline = true;
        }

        await WriteStateAsync();
    }

    public async ValueTask OnDisconnectedAsync(DateTime disconnectTime, bool isAccountSessionSwitched)
    {
        if (State.HashedPassToken == null) return;

        if (State.GameState == 0 && State.SessionId == Guid.Empty)
            return;

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        State.DisconnectionsCount++;
        State.LastActiveTime = DateTimeOffset.UtcNow;

        _tickTimer?.Dispose();
        _tickTimer = null;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (_isCountedInOnline)
        {
            MetricsClient.ObservePlayerDisconnected();
            _isCountedInOnline = false;
        }

        await WriteStateAsync();
    }

    public ValueTask<int> GetGrainGameState()
    {
        return ValueTask.FromResult(State.GameState);
    }

    public ValueTask<int> SetGrainGameState(int gameState)
    {
        return ValueTask.FromResult(State.GameState = gameState);
    }

    public ValueTask<long> GetAccountId()
    {
        return ValueTask.FromResult(State.AccountId);
    }

    public ValueTask<IReadOnlyList<(int messageType, string messageName)>> GetAllImplementedMessagesAsync()
    {
        return _messageManager == null
            ? ValueTask.FromResult<IReadOnlyList<(int messageType, string messageName)>>([])
            : ValueTask.FromResult(MessageManager.GetAllImplementedMessages());
    }

    public async ValueTask<bool> ReceiveImplementedMessagesAsync(PiranhaMessageStruct[] piranhaMessages)
    {
        if (_messageManager == null)
            return false;

        foreach (var message in piranhaMessages)
        {
            var piranhaMessage = LogicLaserMessageFactory.CreateMessageByType(message.MessageType);
            if (piranhaMessage == null) continue;

            LaserContractSerializer.Deserialize(piranhaMessage, message.MessagePayload);

            var res = await _messageManager.ReceiveMessageAsync(piranhaMessage);

            Logger.Debug($"New message: {piranhaMessage.GetMessageTypeName()} received. Result: {res}.");

            if (res < 0)
                return false;
        }

        return true;
    }

    public async ValueTask SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages)
    {
        if (State.GameState == 0 || State.SessionId == Guid.Empty)
            return;

        await SendBridgeEventToClientAsync(
            new BridgeEvent { EventType = 20, SessionId = State.SessionId, PiranhaMessages = messages });

        Logger.Debug($"Sending {messages.Length} messages to {State.SessionId}.");
    }

    public ValueTask<int> LockAccountAsync(string unlockCode)
    {
        if (unlockCode.Length != 12) return ValueTask.FromResult(-1);

        State.IsAccountLocked = true;
        State.AccountUnlockCode = unlockCode;

        return ValueTask.FromResult(0);
    }

    public async ValueTask BanAccountAsync(string reason, DateTime banEndTime)
    {
        State.IsAccountBanned = true;
        State.BanReason = reason;
        State.AccountBanEndTime = banEndTime;

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new LoginFailedMessage
            {
                Capacity = 512,
                ErrorCode = 11,
                Reason = State.BanReason,
                ShowContactSupportForBan = true,
                Tier = 0
            });
    }

    public ValueTask UnbanAndUnlockAccount()
    {
        State.IsAccountBanned = false;
        State.AccountBanEndTime = DateTime.MinValue;
        State.BanReason = "";

        State.IsAccountLocked = false;
        State.AccountUnlockCode = "";

        return ValueTask.CompletedTask;
    }

    public ValueTask<(bool, DateTime, string)> GetBanInfo()
    {
        return ValueTask.FromResult((State.IsAccountBanned, State.AccountBanEndTime, State.BanReason));
    }

    public ValueTask<(bool, string)> GetLockInfo()
    {
        return ValueTask.FromResult((State.IsAccountLocked, State.AccountUnlockCode));
    }

    public ValueTask<int> GetSessionsCount()
    {
        return ValueTask.FromResult(State.SessionsCount);
    }

    public ValueTask<DateTime> GetAccountCreatedTime()
    {
        return ValueTask.FromResult(State.AccountCreatedTime);
    }

    public ValueTask<ulong> GetPlayTimeSeconds()
    {
        return ValueTask.FromResult(State.PlayTimeSeconds);
    }

    public ValueTask<Dictionary<string, int>> GetDevicesInfo()
    {
        return ValueTask.FromResult(State.DevicesInfo);
    }

    public ValueTask<Dictionary<string, int>> GetServersInfo()
    {
        return ValueTask.FromResult(State.ServersInfo);
    }

    public ValueTask<Dictionary<string, int>> GetClientsInfo()
    {
        return ValueTask.FromResult(State.ClientsInfo);
    }

    public async Task<bool> LoginOkAsync()
    {
        if (State.IsAccountLocked)
        {
            var loginFailedMessage = new LoginFailedMessage
            {
                Capacity = 512,
                ErrorCode = 13,
                Reason = "Slishu ZoV - ebu aZoV",
                ShowContactSupportForBan = true,
                Tier = 0
            };

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(loginFailedMessage);

            State.LastKeepAliveReceivedTime = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            return false;
        }

        if (State.IsAccountBanned)
        {
            var secs = (State.AccountBanEndTime - DateTime.UtcNow).TotalSeconds;

            if (secs <= 0)
            {
                State.IsAccountBanned = false;
                State.AccountBanEndTime = DateTime.MinValue;
                State.BanReason = "";
            }
            else
            {
                var loginFailedMessage = new LoginFailedMessage
                {
                    Capacity = 512,
                    ErrorCode = 11,
                    Reason = State.BanReason + $" ({secs} seconds left)",
                    ShowContactSupportForBan = true,
                    Tier = 0
                };

                if (_messageManager != null)
                    await _messageManager.SendMessagesAsync(loginFailedMessage);

                return false;
            }
        }

        var loginOkMessage = new LoginOkMessage
        {
            Capacity = 512,

            AccountId = State.AccountId,
            PassToken = _passToken,
            ServerMajorVersion = Fingerprint.GetMajorVersion(),
            ContentVersion = Fingerprint.GetMajorVersion(),
            ServerBuild = Fingerprint.GetBuildVersion(),
            ServerEnvironment = "prod",
            SessionCount = State.SessionsCount,
            PlayTimeSeconds = (int)State.PlayTimeSeconds,
            DaysSinceStartedPlaying = (DateTime.UtcNow - State.AccountCreatedTime).Days,
            ServerTime = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            AccountCreatedDate = $"{((DateTimeOffset)State.AccountCreatedTime).ToUnixTimeMilliseconds()}",
            StartupCooldownSeconds = 0,
            LoginCountry = "RU",
            Tier = 1,
            GameAssetsUrls =
            [
                "https://game-assets.brawlstarsgame.com"
            ],
            EventAssetsUrls =
            [
                "https://event-assets.brawlstars.com"
            ],
            SecondsUntilAccountDeletion =
                (int)Math.Max(0, (State.AccountRemovedTime - DateTime.UtcNow).TotalSeconds),
            SupercellIdToken = "",
            IsSupercellIdLogoutAllDevicesAllowed = true,
            IsSupercellIdEligible = false
        };

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(loginOkMessage);

        _passToken = null;
        return true;
    }

    public ValueTask<DateTimeOffset> GetLastActiveTime()
    {
        return ValueTask.FromResult(State.LastActiveTime);
    }

    public ValueTask<DateTime> GetLastKeepAliveReceivedTime()
    {
        return ValueTask.FromResult(State.LastKeepAliveReceivedTime);
    }

    public async Task TickAsync(long timestamp)
    {
        if (State.GameState == 0) return;
        if (State.HashedPassToken == null) return;

        try
        {
            State.PlayTimeSeconds += 5;
            State.LastActiveTime = DateTimeOffset.UtcNow;

            if (_messageManager != null)
                await _messageManager.TickAsync();

            if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35)
            {
                await OnDisconnectedAsync(DateTime.UtcNow, false);
                return;
            }

            await WriteStateAsync();
        }
        catch (Exception e)
        {
            var error = e.ToString();

            Logger.Error(error);

            if (_messageManager != null)
                await _messageManager.SendMessagesAndDisconnectAsync(new LoginFailedMessage
                {
                    Capacity = 300,
                    ErrorCode = 1,
                    Reason = error
                });
        }
    }

    public ValueTask<bool> BuildNewAccount(long loginMessageAccountId)
    {
        if (State.AccountCreatedTime != default)
            return ValueTask.FromResult(false);

        State.AccountId = loginMessageAccountId;
        State.AccountCreatedTime = DateTime.UtcNow;
        State.PlayTimeSeconds += 2;

        MetricsClient.ObservePlayerRegistration();
        return ValueTask.FromResult(true);
    }

    public ValueTask KeepAliveAsync()
    {
        State.LastKeepAliveReceivedTime = DateTime.UtcNow;
        return ValueTask.CompletedTask;
    }

    public async Task<(bool, bool)> ReceiveLoginMessageAsync(LoginMessageStruct loginMessage, bool isNewAccount,
        bool isLoginRetry)
    {
        if ((DateTime.UtcNow - State.LastLoginReceivedTime).TotalSeconds < 5 && !isLoginRetry)
            return (false, true);

        State.LastLoginReceivedTime = DateTime.UtcNow;

        if (State.GameState != 0 || State.SessionId != Guid.Empty)
        {
            await DisconnectPlayerAsync(1);

            var time = DateTime.UtcNow;

            // TODO: OnDisconnectedAsync from other grains

            await GrainHelper.GetHomeGrain(GrainFactory, loginMessage.AccountId).OnDisconnectedAsync(time, true);
            await GrainHelper.GetFriendshipGrain(GrainFactory, loginMessage.AccountId).OnDisconnectedAsync(time, true);

            // ReSharper disable once ArrangeThisQualifier
            await this.OnDisconnectedAsync(time, true);

            return (true, false);
        }

        if (!State.DevicesInfo.TryAdd(loginMessage.Device, 1))
            State.DevicesInfo[loginMessage.Device]++;

        State.PlayerDeviceLanguage = loginMessage.PreferredDeviceLanguage;
        State.PlayerLocalizationGlobalId = loginMessage.PreferredLanguage;
        _passToken = loginMessage.PassToken;

        State.LastKeepAliveReceivedTime = DateTime.UtcNow;
        State.LastActiveTime = DateTime.UtcNow;

        return (false, false);
    }

    public async ValueTask<bool> DisconnectPlayerAsync(int? reason)
    {
        if (State.GameState == 0)
            return false;

        if (reason != null)
        {
            var message = new DisconnectedMessage { Capacity = 8, Reason = reason.Value };

            await SendBridgeEventToClientAsync(
                new BridgeEvent
                {
                    EventType = (byte)(reason == 1 ? 40 : 30), SessionId = State.SessionId,
                    PiranhaMessages =
                    [
                        LaserContractSerializer.SerializeToStruct(message)
                    ]
                });

            State.GameState = 0;
            State.SessionId = Guid.Empty;

            return true;
        }

        await SendBridgeEventToClientAsync(new BridgeEvent { EventType = 10, SessionId = State.SessionId });

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        return true;
    }

    public async ValueTask<bool> DisconnectPlayerWithMessagesAsync(PiranhaMessageStruct[] messages)
    {
        if (State.GameState == 0)
            return false;

        await SendBridgeEventToClientAsync(
            new BridgeEvent
            {
                EventType = 30, SessionId = State.SessionId,
                PiranhaMessages = messages
            });

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        return true;
    }

    public ValueTask<int> GetPlayerLocalizationGlobalId()
    {
        return ValueTask.FromResult(State.PlayerLocalizationGlobalId);
    }

    public ValueTask<string> GetPlayerDeviceLanguage()
    {
        return ValueTask.FromResult(State.PlayerDeviceLanguage);
    }

    public ValueTask<bool> CreatePassToken(string passToken)
    {
        if (State.HashedPassToken != null)
            return ValueTask.FromResult(false);

        State.HashedPassToken = HashPassToken(passToken);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> VerifyPassToken(string passToken)
    {
        var hp = HashPassToken(passToken);

        return ValueTask.FromResult(State.HashedPassToken != null && State.HashedPassToken.SequenceEqual(hp));
    }

    public Task RegisterBridgeObserverAsync(IBridgeObserver observer)
    {
        _bridgeObserver = observer;
        return Task.CompletedTask;
    }

    public Task UnregisterBridgeObserverAsync(IBridgeObserver observer)
    {
        if (_bridgeObserver?.Equals(observer) == true)
            _bridgeObserver = null;

        return Task.CompletedTask;
    }

    public async Task SendBridgeEventToClientAsync(BridgeEvent bridgeEvent)
    {
        if (_bridgeObserver != null)
            try
            {
                await _bridgeObserver.SendBridgeEventAsync(bridgeEvent);
            }
            catch (Exception ex)
            {
                _bridgeObserver = null;

                Logger.Warn($"Failed to send bridge event to client: {ex.Message}");
                throw;
            }
    }

    private static byte[] HashPassToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        var hash1 = SHA512.HashData(tokenBytes);

        var hash2 = SHA384.HashData(hash1);

        var xorHash = new byte[hash1.Length];
        for (var i = 0; i < xorHash.Length; i++)
            xorHash[i] = (byte)(hash1[i] ^ hash2[i % hash2.Length]);

        var hash3 = SHA256.HashData(xorHash);

        using var hmac = new HMACSHA512(hash3.Take(64).ToArray());

        return hmac.ComputeHash(tokenBytes);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var accountId = this.GetPrimaryKeyString().Split('_').Last();
        State.AccountId = Convert.ToInt64(accountId);

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (State.HashedPassToken == null) return;

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        _tickTimer?.Dispose();
        _tickTimer = null;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (_isCountedInOnline)
        {
            MetricsClient.ObservePlayerDisconnected();
            _isCountedInOnline = false;
        }

        await WriteStateAsync();

        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}