using NLog;
using Orleans.Providers;
using ZOVserver.Services.Game.FriendshipService.Laser.Messages;
using ZOVserver.Services.Game.FriendshipService.Settings;
using ZOVserver.Services.Game.FriendshipService.States;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Helper;
using ZOVserver.Shared.Contracts.Interfaces;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.Contracts.Laser.Messages;
using ZOVserver.Shared.Contracts.Laser.Messages.Server;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Services.Game.FriendshipService.Grains;

[StorageProvider(ProviderName = "MongoStorage")]
// ReSharper disable once UnusedType.Global
public class FriendshipGrain : Grain<FriendshipState>, IFriendshipServiceGrain
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private IBridgeObserver? _bridgeObserver;
    private MessageManager? _messageManager;
    private IDisposable? _tickTimer;

    public async ValueTask OnConnectedAsync(string serverIp, int serverPort, string clientIp, int clientPort,
        Guid sessionId)
    {
        State.GameState = 1;
        State.SessionId = sessionId;

        if (State.FriendshipCreatedTime != default)
            _tickTimer ??= this.RegisterGrainTimer<object?>(
                async _ => await TickAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                null,
                new GrainTimerCreationOptions
                {
                    DueTime = TimeSpan.FromMinutes(2),
                    Period = TimeSpan.FromMinutes(5),
                    Interleave = false
                });

        var playerSession = GrainHelper.GetPlayerSession(GrainFactory, State.AccountId);

        _messageManager ??=
            new MessageManager(this, State, playerSession, GrainFactory);

        State.LastKeepAliveReceivedTime = DateTime.UtcNow;

        if (State.FriendshipCreatedTime != default)
            await WriteStateAsync();
    }

    public async ValueTask OnDisconnectedAsync(DateTime disconnectTime, bool isAccountSessionSwitched)
    {
        if (State.GameState == 0 && State.SessionId == Guid.Empty)
            return;

        State.GameState = 0;
        State.SessionId = Guid.Empty;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (State.FriendshipCreatedTime != default)
            await WriteStateAsync();
    }

    public ValueTask<long> GetAccountId()
    {
        return ValueTask.FromResult(State.AccountId);
    }

    public ValueTask<int> GetGrainGameState()
    {
        return ValueTask.FromResult(State.GameState);
    }

    public ValueTask<int> SetGrainGameState(int gameState)
    {
        return ValueTask.FromResult(State.GameState = gameState);
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

    public async ValueTask<bool> SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages)
    {
        if (State.GameState == 0 || State.SessionId == Guid.Empty)
            return false;

        await SendBridgeEventToClientAsync(
            new BridgeEvent { EventType = 20, SessionId = State.SessionId, PiranhaMessages = messages });

        Logger.Debug($"Sending {messages.Length} messages to {State.SessionId}.");
        return true;
    }

    public async ValueTask<(int, FriendEntry?)> SyncFriendAsync(int friendState, FriendEntry friendEntry,
        bool fullFriends)
    {
        if (!State.Friends.TryGetValue(friendEntry.AccountId, out var friend))
            return (-1, null);

        switch (friendState)
        {
            case 2 when friend.FriendState != 3:
            {
                if (friend.FriendState == 4 && !fullFriends)
                {
                    friendEntry.FriendState = 4;
                    friendEntry.FriendReason = 0;

                    State.Friends[friendEntry.AccountId] = friendEntry;
                    State.Suggestions.Remove(friendEntry.AccountId);

                    return (1004, State.MyFriendEntry);
                }

                friend.FriendState = 0;

                State.Friends.Remove(friend.AccountId);
                State.Suggestions.Remove(friend.AccountId);

                return (-2, null);
            }
            case 2:
            {
                friendEntry.FriendState = friend.FriendState;
                friendEntry.FriendReason = friend.FriendReason;

                State.Friends[friendEntry.AccountId] = friendEntry;
                return (1005, State.MyFriendEntry);
            }
            case 3 when friend.FriendState != 2:
            {
                friend.FriendState = 0;

                State.Friends.Remove(friend.AccountId);
                State.Suggestions.Remove(friend.AccountId);

                return (-3, null);
            }
            case 3:
            {
                friendEntry.FriendState = friend.FriendState;
                friendEntry.FriendReason = friend.FriendReason;

                State.Friends[friendEntry.AccountId] = friendEntry;
                return (2000, State.MyFriendEntry);
            }
            case 4:
            {
                switch (friend.FriendState)
                {
                    case 4:
                    {
                        friendEntry.FriendState = 4;
                        friendEntry.FriendReason = 0;

                        State.Friends[friendEntry.AccountId] = friendEntry;
                        State.Suggestions.Remove(friendEntry.AccountId);

                        return (3000, State.MyFriendEntry);
                    }
                    case 2 when
                        State.Friends.Count(x => x.Value.FriendState == 4) >=
                        FriendshipSettings.GetConfig().MaxFriendsCount:
                    {
                        friend.FriendState = 0;

                        State.Friends.Remove(friend.AccountId);
                        State.Suggestions.Remove(friend.AccountId);

                        return (-4, null);
                    }
                    case 2:
                    {
                        friendEntry.FriendState = 4;
                        friendEntry.FriendReason = 0;

                        State.Friends[friendEntry.AccountId] = friendEntry;
                        State.Suggestions.Remove(friendEntry.AccountId);

                        if (_messageManager != null)
                            await _messageManager.SendMessagesAsync(
                                new FriendListUpdateMessage { FriendEntry = friendEntry, UnkBool = true });

                        return (3001, State.MyFriendEntry);
                    }
                }

                friend.FriendState = 0;

                State.Friends.Remove(friend.AccountId);
                State.Suggestions.Remove(friend.AccountId);

                return (-5, null);
            }
            default:
            {
                friend.FriendState = 0;

                State.Friends.Remove(friend.AccountId);
                State.Suggestions.Remove(friend.AccountId);

                return (-6, null);
            }
        }
    }

    public async Task TickAsync(long timestamp)
    {
        try
        {
            Interlocked.Exchange(ref State.MyFriendRequestsCount, 0);

            await SyncFriendsAndSuggestionsAsync();
            await SyncFriendEntriesAsync();

            if (_messageManager != null)
                await _messageManager.TickAsync();

            if ((DateTime.UtcNow - State.LastKeepAliveReceivedTime).TotalSeconds > 35 && State.GameState != 0)
            {
                await OnDisconnectedAsync(DateTime.UtcNow, false);
                return;
            }

            if (State.FriendshipCreatedTime != default)
                await WriteStateAsync();
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
    }

    public ValueTask<bool> BuildNewAccount(long loginMessageAccountId)
    {
        if (State.FriendshipCreatedTime != default)
            return ValueTask.FromResult(false);

        State.AccountId = loginMessageAccountId;
        State.FriendshipCreatedTime = DateTime.UtcNow;
        return ValueTask.FromResult(true);
    }

    public ValueTask KeepAliveAsync()
    {
        State.LastKeepAliveReceivedTime = DateTime.UtcNow;
        return ValueTask.CompletedTask;
    }

    public ValueTask<DateTime> GetLastKeepAliveReceivedTime()
    {
        return ValueTask.FromResult(State.LastKeepAliveReceivedTime);
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

    public ValueTask<(FriendEntry?, bool)> GetMyFriendEntryAndBlockRequestState()
    {
        return ValueTask.FromResult((State.MyFriendEntry, State.FriendRequestsBlocked));
    }

    public ValueTask<FriendEntry?> GetFriendEntry(long accountId)
    {
        return State.Friends.TryGetValue(accountId, out var friend)
            ? new ValueTask<FriendEntry?>(friend)
            : default;
    }

    public ValueTask<bool> IsMyFriend(long accountId)
    {
        return !State.Friends.TryGetValue(accountId, out var friendEntry)
            ? ValueTask.FromResult(false)
            : ValueTask.FromResult(friendEntry.FriendState == 4);
    }

    public async ValueTask<(int?, int?)> GetFriendStateAndReason(long accountId)
    {
        var friend = await GetFriendEntry(accountId);

        if (friend == null)
            return (null, null);

        return (friend.FriendState, friend.FriendReason);
    }

    public async Task ChangeFriendEntryAsync(FriendEntry friendEntry)
    {
        if (State.AccountId == friendEntry.AccountId)
        {
            State.MyFriendEntry = friendEntry;

            var cc = State.Friends.Values
                .Where(x => x.FriendState == 4 && x.AccountId != friendEntry.AccountId)
                .ToList();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var friend in cc)
            {
                var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, friend.AccountId);
                await friendship.ChangeFriendEntryAsync(friendEntry);
            }

            return;
        }

        if (!State.Friends.TryGetValue(friendEntry.AccountId, out var oldFriendEntry))
            return;

        friendEntry.FriendState = oldFriendEntry.FriendState;
        friendEntry.FriendReason = oldFriendEntry.FriendReason;

        State.Friends[friendEntry.AccountId] = friendEntry;

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new FriendListUpdateMessage { FriendEntry = friendEntry });
    }

    public async ValueTask ChangeMyFriendOnlineStatusEntryAsync(long accountId,
        FriendOnlineStatusEntry? statusEntry)
    {
        if (State.AccountId != accountId)
            return;

        if (statusEntry is { AllianceTeamEntry.Players.Length: 0 })
            statusEntry.AllianceTeamEntry = null;

        var friends = State.Friends.Values.ToArray();

        if (State.CachedOnlineStatusEntry == null && statusEntry != null)
            if (_messageManager != null)
            {
                var tasks = friends
                    .Where(x => x.FriendState == 4)
                    .Select(f => GetFriendOnlineStatusAsync(f.AccountId))
                    .ToArray();

                var res = await Task.WhenAll(tasks);

                var sts = res
                    .Where(z => z != null)
                    .Select(PiranhaMessage (friendStatus) => new FriendOnlineStatusEntryMessage
                        { AccountId = friendStatus!.AccountId, FriendOnlineStatusEntry = friendStatus })
                    .ToList();

                sts.Add(new FriendListMessage
                {
                    FriendRequestsBlocked = State.FriendRequestsBlocked,
                    FriendEntries = friends
                });

                await _messageManager.SendMessagesAsync(sts.ToArray());
            }

        State.CachedOnlineStatusEntry = statusEntry;

        PiranhaMessageStruct[] msgs =
        [
            LaserContractSerializer.SerializeToStruct(new FriendOnlineStatusEntryMessage
                { AccountId = accountId, FriendOnlineStatusEntry = statusEntry })
        ];

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var friendEntry in friends.Where(x => x.FriendState == 4).ToList())
        {
            var playerSession = GrainHelper.GetPlayerSession(GrainFactory, friendEntry.AccountId);
            await playerSession.SendMessagesToPlayerAsync(msgs);
        }

        return;

        async Task<FriendOnlineStatusEntry?> GetFriendOnlineStatusAsync(long playerId)
        {
            try
            {
                var fship = GrainHelper.GetFriendshipGrain(GrainFactory, playerId);

                return await fship.GetMyFriendOnlineStatusEntry();
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
                return null;
            }
        }
    }

    public ValueTask<FriendOnlineStatusEntry?> GetMyFriendOnlineStatusEntry()
    {
        return ValueTask.FromResult(State.CachedOnlineStatusEntry);
    }

    public ValueTask<FriendEntry[]> GetFriendEntries()
    {
        return new ValueTask<FriendEntry[]>(State.Friends.Select(x => x.Value).ToArray());
    }

    public async ValueTask<(int, FriendEntry?)> AddFriendRequestAsync(FriendEntry friendEntry)
    {
        if (State.MyFriendEntry == null)
            return (-1, null);

        if (friendEntry.FriendState != 3)
            return (-2, null);

        if (friendEntry.AccountId == State.AccountId)
            return (-3, null);

        if (State.Friends.Count(x => x.Value.FriendState == 3) >=
            FriendshipSettings.GetConfig().MaxFriendRequestsCount)
            return (-4, null);

        if (State.Friends.Count(x => x.Value.FriendState == 4) >=
            FriendshipSettings.GetConfig().MaxFriendsCount)
            return (-5, null);

        if (State.FriendRequestsBlocked)
            return (-6, null);

        var res = State.Friends.TryAdd(friendEntry.AccountId, friendEntry);

        if (!res)
            return (-7, null);

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                { FriendEntry = friendEntry, UnkBool = true });

        return (0, State.MyFriendEntry);
    }

    public ValueTask<int> AddSuggestionAsync(SuggestionEntry suggestionEntry)
    {
        if (suggestionEntry.FriendEntry == null)
            return ValueTask.FromResult(-1);

        if (suggestionEntry.FriendEntry.AccountId == State.AccountId)
            return ValueTask.FromResult(-2);

        if (State.Friends.ContainsKey(suggestionEntry.FriendEntry.AccountId))
            return ValueTask.FromResult(-3);

        if (State.Suggestions.ContainsKey(suggestionEntry.FriendEntry.AccountId))
            return ValueTask.FromResult(-4);

        var maxCount = FriendshipSettings.GetConfig().MaxSuggestionsCount;

        while (State.Suggestions.Count > maxCount)
            State.Suggestions.Remove(State.Suggestions.Keys.First());

        return ValueTask.FromResult(State.Suggestions.TryAdd(suggestionEntry.FriendEntry.AccountId, suggestionEntry)
            ? 0
            : -5);
    }

    public async ValueTask<(int, FriendOnlineStatusEntry?)> FriendRequestAcceptedAsync(long acceptorId)
    {
        var f = State.Friends.TryGetValue(acceptorId, out var friendEntry);

        if (!f)
            return (-1, null);

        if (friendEntry?.FriendState != 2)
            return (-2, null);

        if (State.Friends.Count(x => x.Value.FriendState == 4) >=
            FriendshipSettings.GetConfig().MaxFriendsCount)
            return (-3, null);

        friendEntry.FriendState = 4;
        friendEntry.FriendReason = 0;

        State.Suggestions.Remove(acceptorId);

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                { FriendEntry = friendEntry, UnkBool = true });

        return (0, State.CachedOnlineStatusEntry);
    }

    public async ValueTask<int> FriendRequestRejectedAsync(long rejecterId)
    {
        var f = State.Friends.TryGetValue(rejecterId, out var friendEntry);

        if (!f)
            return -1;

        if (friendEntry?.FriendState != 2)
            return -2;

        friendEntry.FriendReason = 0;
        friendEntry.FriendState = 0;

        State.Friends.Remove(rejecterId);
        State.Suggestions.Remove(rejecterId);

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                { FriendEntry = friendEntry, UnkBool = true });

        return 0;
    }

    public async ValueTask<int> RemoveFriendAsync(long removeId)
    {
        if (!State.Friends.Remove(removeId, out var friendEntry))
            return -1;

        friendEntry.FriendState = 0;
        friendEntry.FriendReason = 0;

        State.Suggestions.Remove(removeId);

        if (_messageManager != null)
            await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                { FriendEntry = friendEntry, UnkBool = true });

        return 0;
    }

    public ValueTask<DateTime> GetFriendshipCreatedTime()
    {
        return ValueTask.FromResult(State.FriendshipCreatedTime);
    }

    public ValueTask<float> CalculateSuggestionRelevanceAsync(FriendEntry potentialFriend)
    {
        if (State.MyFriendEntry == null ||
            potentialFriend.AccountId == State.AccountId ||
            State.Friends.ContainsKey(potentialFriend.AccountId) ||
            State.Suggestions.ContainsKey(potentialFriend.AccountId))
            return ValueTask.FromResult(0f);

        var score = 0f;

        var sameAlliance = State.MyFriendEntry.Alliance != null &&
                           potentialFriend.Alliance != null &&
                           State.MyFriendEntry.Alliance.AllianceId == potentialFriend.Alliance.AllianceId;

        if (sameAlliance)
            score += 0.4f;

        var trophyDiff = Math.Abs(State.MyFriendEntry.Trophies - potentialFriend.Trophies);

        if (trophyDiff <= 10000)
        {
            var trophyScore = 0.35f * (1 - trophyDiff / 10000f);
            score += Math.Max(trophyScore, 0.05f);
        }

        var isOnline = potentialFriend.LastOnlineTime == default;

        if (isOnline)
        {
            score += 0.25f;
        }
        else
        {
            var hoursOffline = (DateTime.UtcNow - potentialFriend.LastOnlineTime).TotalHours;

            if (hoursOffline <= 24)
            {
                var activityScore = 0.25f * (1 - (float)hoursOffline / 24);
                score += Math.Max(activityScore, 0.05f);
            }
        }

        if (isOnline && State.MyFriendEntry.LastOnlineTime == default)
            score += 0.15f;

        if (potentialFriend.Trophies > 10000 && State.MyFriendEntry.Trophies > 10000)
            score += 0.1f;

        if (!isOnline && (DateTime.UtcNow - potentialFriend.LastOnlineTime).TotalDays > 7)
            score *= 0.7f;

        if (isOnline || (DateTime.UtcNow - potentialFriend.LastOnlineTime).TotalHours <= 2)
            score = Math.Max(score, 0.1f);

        return ValueTask.FromResult(Math.Min(score, 1f));
    }

    public async Task<int> CreateFriendRequestAsync(long playerId, int reason)
    {
        if (State.MyFriendEntry == null)
            return -1;

        if (reason is not (0 or 1 or 2 or 3 or 4))
            return -2;

        if (State.AccountId == playerId)
            return -3;

        if (State.Friends.TryGetValue(playerId, out var of))
            return of.FriendState == 2 ? -4 : -5;

        if (State.Friends.Count(x => x.Value.FriendState == 4) >=
            FriendshipSettings.GetConfig().MaxFriendsCount)
            return -6;

        if (Interlocked.Increment(ref State.MyFriendRequestsCount) > 60)
            return -7;

        try
        {
            var targetFriendship = GrainHelper.GetFriendshipGrain(GrainFactory, playerId);

            var res = await targetFriendship.AddFriendRequestAsync(new FriendEntry
            {
                AccountId = State.AccountId,
                Trophies = State.MyFriendEntry.Trophies,
                FriendState = 3,
                FriendReason = reason,
                Alliance = State.MyFriendEntry.Alliance,
                LastOnlineTime = State.MyFriendEntry.LastOnlineTime,
                DisplayData = State.MyFriendEntry.DisplayData
            });

            if (res is not { Item1: 0, Item2: not null })
                return -7 + res.Item1;

            res.Item2.FriendState = 2;
            res.Item2.FriendReason = reason;

            State.Friends.Add(playerId, res.Item2);

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                    { FriendEntry = res.Item2, UnkBool = true });

            return 0;
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
            return -15;
        }
    }

    public async Task<int> AcceptFriendRequestAsync(long accountId)
    {
        if (State.MyFriendEntry == null)
            return -1;

        if (!State.Friends.TryGetValue(accountId, out var fr))
            return -2;

        if (fr.FriendState != 3)
            return -3;

        if (State.Friends.Count(x => x.Value.FriendState == 4) >=
            FriendshipSettings.GetConfig().MaxFriendsCount)
            return -4;

        try
        {
            var targetFriendship = GrainHelper.GetFriendshipGrain(GrainFactory, accountId);

            var res = await targetFriendship.FriendRequestAcceptedAsync(State.AccountId);

            if (res.Item1 != 0)
                return -4 + res.Item1;

            fr.FriendState = 4;
            fr.FriendReason = 0;

            State.Suggestions.Remove(fr.AccountId);

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(
                    new FriendListUpdateMessage { FriendEntry = fr, UnkBool = true },
                    new FriendOnlineStatusEntryMessage
                        { AccountId = fr.AccountId, FriendOnlineStatusEntry = res.Item2 });

            return 0;
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
            return -8;
        }
    }

    public async Task<int> RejectFriendRequestAsync(long accountId)
    {
        if (State.MyFriendEntry == null)
            return -1;

        if (!State.Friends.TryGetValue(accountId, out var fr))
            return -2;

        if (fr.FriendState != 3)
            return -3;

        try
        {
            var targetFriendship = GrainHelper.GetFriendshipGrain(GrainFactory, accountId);

            var res = await targetFriendship.FriendRequestRejectedAsync(State.AccountId);

            if (res != 0)
                return -3 + res;

            fr.FriendState = 0;
            fr.FriendReason = 0;

            State.Friends.Remove(fr.AccountId);
            State.Suggestions.Remove(fr.AccountId);

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(
                    new FriendListUpdateMessage { FriendEntry = fr, UnkBool = true });

            return 0;
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
            return -6;
        }
    }

    public async Task<int> RemoveMyFriendAsync(long accountId)
    {
        if (State.MyFriendEntry == null)
            return -1;

        if (!State.Friends.TryGetValue(accountId, out var friendEntry))
            return -2;

        try
        {
            var targetFriendship = GrainHelper.GetFriendshipGrain(GrainFactory, accountId);

            var res = await targetFriendship.RemoveFriendAsync(State.AccountId);

            if (res != 0)
                return -2 + res;

            friendEntry.FriendState = 0;
            friendEntry.FriendReason = 0;

            State.Friends.Remove(accountId);
            State.Suggestions.Remove(accountId);

            if (_messageManager != null)
                await _messageManager.SendMessagesAsync(new FriendListUpdateMessage
                    { FriendEntry = friendEntry, UnkBool = true });

            return 0;
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
            return -4;
        }
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var accountId = this.GetPrimaryKeyString().Split('_').Last();
        State.AccountId = Convert.ToInt64(accountId);

        if (State.FriendshipCreatedTime != default)
            _tickTimer ??= this.RegisterGrainTimer<object?>(
                async _ => await TickAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                null,
                new GrainTimerCreationOptions
                {
                    DueTime = TimeSpan.FromMinutes(2),
                    Period = TimeSpan.FromMinutes(5),
                    Interleave = false
                });

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        State.GameState = 0;
        State.SessionId = Guid.Empty;

        _tickTimer?.Dispose();
        _tickTimer = null;

        if (_messageManager != null)
            await _messageManager.GoodbyeAsync();
        _messageManager = null;

        if (State.FriendshipCreatedTime != default)
            await WriteStateAsync();

        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task SyncFriendEntriesAsync()
    {
        try
        {
            var home = GrainHelper.GetHomeGrain(GrainFactory, State.AccountId);
            var (friendEntry, friendOnlineStatusEntry) = await home.SyncFriendshipWithHomeAsync();

            if (State.MyFriendEntry == null)
            {
                State.MyFriendEntry = friendEntry;
                State.CachedOnlineStatusEntry = friendOnlineStatusEntry;
                return;
            }

            if (FriendEntryChanged(State.MyFriendEntry, friendEntry))
                await ChangeFriendEntryAsync(friendEntry);

            if (OnlineStatusChanged(State.CachedOnlineStatusEntry, friendOnlineStatusEntry))
                await ChangeMyFriendOnlineStatusEntryAsync(State.AccountId, friendOnlineStatusEntry);

            State.MyFriendEntry = friendEntry;
            State.CachedOnlineStatusEntry = friendOnlineStatusEntry;
        }
        catch (Exception e)
        {
            Logger.Warn(e.ToString());
        }
    }

    private static bool FriendEntryChanged(FriendEntry oldEntry, FriendEntry newEntry)
    {
        return oldEntry.LastOnlineTime != newEntry.LastOnlineTime ||
               oldEntry.Trophies != newEntry.Trophies ||
               !DisplayDataEquals(oldEntry.DisplayData, newEntry.DisplayData) ||
               !AllianceEquals(oldEntry.Alliance, newEntry.Alliance);
    }

    private static bool OnlineStatusChanged(FriendOnlineStatusEntry? oldStatus, FriendOnlineStatusEntry? newStatus)
    {
        if (oldStatus == null && newStatus == null) return false;
        if (oldStatus == null || newStatus == null) return true;

        return oldStatus.Status != newStatus.Status ||
               oldStatus.InvitesBlocked != newStatus.InvitesBlocked ||
               !AllianceTeamEquals(oldStatus.AllianceTeamEntry, newStatus.AllianceTeamEntry);
    }

    private static bool DisplayDataEquals(PlayerDisplayData? a, PlayerDisplayData? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.AvatarName == b.AvatarName &&
               a.Experience == b.Experience &&
               a.Thumbnail == b.Thumbnail &&
               a.NameColor == b.NameColor;
    }

    private static bool AllianceEquals(FriendAllianceSegment? a, FriendAllianceSegment? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.AllianceId == b.AllianceId;
    }

    private static bool AllianceTeamEquals(AllianceTeamEntry? a, AllianceTeamEntry? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.TeamId == b.TeamId &&
               a.OwnerAccountId == b.OwnerAccountId &&
               a.RoomType == b.RoomType &&
               a.MaxPlayers == b.MaxPlayers &&
               PlayersEqualAsSets(a.Players, b.Players);
    }

    private static bool PlayersEqualAsSets(long[]? a, long[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        var set = new HashSet<long>(a);
        return set.SetEquals(b);
    }

    private async Task SyncFriendsAndSuggestionsAsync()
    {
        if (State.MyFriendEntry == null)
            return;

        var friends = State.Friends.ToArray();

        var fullFriends = friends
                              .Count(x => x.Value.FriendState == 4) >=
                          FriendshipSettings.GetConfig().MaxFriendsCount;

        var networkTasks = new Dictionary<long, Task<(int, FriendEntry?)>?>();

        foreach (var (key, friendEntry) in friends)
            try
            {
                var friendship = GrainHelper.GetFriendshipGrain(GrainFactory, key);

                networkTasks[key] = friendship
                    .SyncFriendAsync(friendEntry.FriendState, State.MyFriendEntry, fullFriends)
                    .AsTask();
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
                networkTasks[key] = null;
            }

        var tasks = networkTasks.Values
            .Where(t => t != null)
            .Select(t => t!);

        await Task.WhenAll(tasks);

        foreach (var (key, friendEntry) in friends)
        {
            try
            {
                if (!networkTasks.TryGetValue(key, out var task) || task == null)
                    continue;

                var res = task.Result;

                switch (res.Item1)
                {
                    case -1 or -2 or -3 or -4 or -5 or -6:
                    {
                        friendEntry.FriendState = 0;

                        State.Friends.Remove(friendEntry.AccountId);
                        State.Suggestions.Remove(friendEntry.AccountId);

                        break;
                    }
                    default:
                    {
                        if (res.Item2 == null)
                            break;

                        switch (res.Item1)
                        {
                            case 1004 or 3000 or 3001:
                            {
                                res.Item2.FriendState = 4;
                                res.Item2.FriendReason = 0;

                                State.Friends[friendEntry.AccountId] = res.Item2;
                                State.Suggestions.Remove(friendEntry.AccountId);

                                if (res.Item1 == 1004 && _messageManager != null)
                                    await _messageManager.SendMessagesAsync(
                                        new FriendListUpdateMessage { FriendEntry = res.Item2, UnkBool = true });

                                break;
                            }
                            case 1005 or 2000:
                            {
                                res.Item2.FriendState = friendEntry.FriendState;
                                res.Item2.FriendReason = friendEntry.FriendReason;

                                State.Friends[friendEntry.AccountId] = res.Item2;

                                break;
                            }
                        }

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e.ToString());
            }

            if (friendEntry.FriendState == 4)
                friendEntry.FriendReason = 0;
        }

        foreach (var (key, _) in State.Suggestions.ToArray())
        {
            if (!State.Friends.TryGetValue(key, out var friendEntry))
                continue;

            if (friendEntry.FriendState == 4)
                State.Suggestions.Remove(key);
        }
    }
}