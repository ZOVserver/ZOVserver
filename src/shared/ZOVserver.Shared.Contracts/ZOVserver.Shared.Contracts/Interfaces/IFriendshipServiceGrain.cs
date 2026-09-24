using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IFriendshipServiceGrain")]
public interface IFriendshipServiceGrain : IGrainWithStringKey
{
    /// <summary>
    ///     Handles the event when a client is connected.
    /// </summary>
    /// <param name="serverIp">The IP address of the server.</param>
    /// <param name="serverPort">The port number of the server.</param>
    /// <param name="clientIp">The IP address of the connecting client.</param>
    /// <param name="clientPort">The port number used by the connecting client.</param>
    /// <param name="sessionId">Unique identifier for the session.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    [Alias("OnConnected")]
    [OneWay]
    ValueTask OnConnectedAsync(
        [Immutable] string serverIp,
        int serverPort,
        [Immutable] string clientIp,
        int clientPort,
        Guid sessionId);

    /// <summary>
    ///     Handles client disconnection.
    ///     This method is intended to be called (OneWay) by clients.
    /// </summary>
    /// <param name="disconnectTime">Time when disconnection occurred.</param>
    /// <param name="isAccountSessionSwitched">
    ///     True if the account was disconnected because a new session was opened.
    /// </param>
    /// <returns></returns>
    [Alias("OnDisconnected")]
    [OneWay]
    public ValueTask OnDisconnectedAsync(DateTime disconnectTime, bool isAccountSessionSwitched);

    /// <summary>
    ///     Gets the account ID associated with this grain.
    ///     This method is marked as read-only and can execute in parallel with other operations
    ///     due to the <see cref="ReadOnlyAttribute" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask{Int64}" /> that represents the asynchronous operation.
    ///     The task result contains the account ID as a 64-bit integer.
    /// </returns>
    /// <remarks>
    ///     The method is aliased as "GetAccountID" for backward compatibility.
    ///     Safe to call concurrently with other grain methods.
    /// </remarks>
    [Alias("GetAccountID")]
    [ReadOnly]
    public ValueTask<long> GetAccountId();

    /// <summary>
    ///     Retrieves a list of all implemented message types and their corresponding names.
    ///     This method can execute in parallel with other methods due to the AlwaysInterleave attribute.
    /// </summary>
    /// <returns>
    ///     A ValueTask that represents the asynchronous operation.
    ///     The result is an array of tuples, each containing:
    ///     - messageType: numeric identifier of the message type.
    ///     - messageName: human-readable name of the message.
    /// </returns>
    [Alias("GetAllMsgImpls")]
    [AlwaysInterleave]
    public ValueTask<IReadOnlyList<(int messageType, string messageName)>> GetAllImplementedMessagesAsync();

    /// <summary>
    ///     Gets the current game state of the grain.
    ///     This method can execute in parallel with other methods due to the AlwaysInterleave attribute.
    /// </summary>
    /// <returns>A value representing the current game state.</returns>
    [Alias("GetGGameState")]
    [AlwaysInterleave]
    public ValueTask<int> GetGrainGameState();

    /// <summary>
    ///     Sets a new game state for the grain.
    ///     This method can execute in parallel with other methods due to the AlwaysInterleave attribute.
    /// </summary>
    /// <param name="gameState">New game state.</param>
    /// <returns>A value representing the current game state.</returns>
    [Alias("SetGGameState")]
    [AlwaysInterleave]
    public ValueTask<int> SetGrainGameState(int gameState);

    /// <summary>
    ///     Handles the reception and processing of an implemented messages sent to the grain.
    /// </summary>
    /// <param name="piranhaMessages">The message structures containing header and payload.</param>
    /// <returns>
    ///     A ValueTask that represents the asynchronous operation.
    ///     The result indicates whether the messages were successfully processed (true) or not (false).
    /// </returns>
    [Alias("RcvImplMsgspm")]
    public ValueTask<bool> ReceiveImplementedMessagesAsync(PiranhaMessageStruct[] piranhaMessages);

    /// <summary>
    ///     Sends messages to the player asynchronously.
    /// </summary>
    /// <param name="messages">An array of messages to be sent to the player.</param>
    /// <returns>
    ///     A ValueTask that represents the asynchronous operation.
    ///     The result indicates whether the messages were successfully sent (true) or not (false).
    /// </returns>
    [Alias("SendMsgsToPlr")]
    public ValueTask<bool> SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages);

    /// <summary>
    ///     A method for periodic internal management.
    /// </summary>
    /// <param name="timestamp">UTC Time in Seconds.</param>
    /// <returns></returns>
    [Alias("Tick")]
    public Task TickAsync(long timestamp);

    /// <summary>
    ///     A method for creating a new account.
    /// </summary>
    /// <returns>
    ///     A ValueTask that represents the asynchronous operation.
    ///     The result indicates whether the creating account successfully processed (true) or not (false).
    /// </returns>
    [Alias("BuildNewAcc")]
    public ValueTask<bool> BuildNewAccount(long loginMessageAccountId);

    /// <summary>
    ///     A method for keeping the connection alive.
    /// </summary>
    /// <returns></returns>
    [Alias("KeepAlive")]
    public ValueTask KeepAliveAsync();

    /// <summary>
    ///     Retrieves the last keep-alive received time of the player.
    /// </summary>
    /// <returns></returns>
    [Alias("GetLKAReceivedTime")]
    [ReadOnly]
    public ValueTask<DateTime> GetLastKeepAliveReceivedTime();

    [Alias("RegisterBridgeObserver")]
    Task RegisterBridgeObserverAsync(IBridgeObserver observer);

    [Alias("UnregisterBridgeObserver")]
    Task UnregisterBridgeObserverAsync(IBridgeObserver observer);

    [Alias("SendBridgeEventToClient")]
    Task SendBridgeEventToClientAsync(BridgeEvent bridgeEvent);

    [Alias("ChangeFriendEntry")]
    [OneWay]
    public Task ChangeFriendEntryAsync(FriendEntry friendEntry);

    [Alias("SyncFriend")]
    public ValueTask<(int, FriendEntry?)> SyncFriendAsync(int friendState, FriendEntry friendEntry, bool fullFriends);

    [ReadOnly]
    [Alias("GetMyFriendEntryAndBlockRequestState")]
    public ValueTask<(FriendEntry?, bool)> GetMyFriendEntryAndBlockRequestState();

    [ReadOnly]
    [Alias("GetFriendStateAndReason")]
    public ValueTask<(int?, int?)> GetFriendStateAndReason(long accountId);

    [ReadOnly]
    [Alias("GetFriendEntry")]
    public ValueTask<FriendEntry?> GetFriendEntry(long accountId);

    [ReadOnly]
    [Alias("IsMyFriend")]
    public ValueTask<bool> IsMyFriend(long accountId);

    [Alias("ChangeMyFriendOnlineStatusEntry")]
    [OneWay]
    public ValueTask ChangeMyFriendOnlineStatusEntryAsync(long accountId,
        FriendOnlineStatusEntry? statusEntry);

    [ReadOnly]
    [Alias("GetMyFriendOnlineStatusEntry")]
    public ValueTask<FriendOnlineStatusEntry?> GetMyFriendOnlineStatusEntry();

    [ReadOnly]
    [Alias("GetFriendEntries")]
    public ValueTask<FriendEntry[]> GetFriendEntries();

    [Alias("AddFriendRequest")]
    public ValueTask<(int, FriendEntry?)> AddFriendRequestAsync(FriendEntry friendEntry);

    [Alias("AddSuggestion")]
    public ValueTask<int> AddSuggestionAsync(SuggestionEntry suggestionEntry);

    [Alias("FriendRequestAccepted")]
    public ValueTask<(int, FriendOnlineStatusEntry?)> FriendRequestAcceptedAsync(long acceptorId);

    [Alias("FriendRequestRejected")]
    public ValueTask<int> FriendRequestRejectedAsync(long rejecterId);

    [Alias("RemoveFriend")]
    public ValueTask<int> RemoveFriendAsync(long removeId);

    [Alias("GetFriendshipCreatedTime")]
    [ReadOnly]
    public ValueTask<DateTime> GetFriendshipCreatedTime();
}