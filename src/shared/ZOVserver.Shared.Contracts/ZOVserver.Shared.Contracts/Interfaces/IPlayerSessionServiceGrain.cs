using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Shared.Contracts.Interfaces;

/// <summary>
///     Interface for managing player-related operations.
/// </summary>
[Alias("ZOVserver.Shared.Contracts.Interfaces.ILoginServiceGrain")]
public interface IPlayerSessionServiceGrain : IGrainWithStringKey
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
    /// </returns>
    [Alias("SendMsgsToPlr")]
    [OneWay]
    public ValueTask SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages);

    /// <summary>
    ///     A method for periodic internal management.
    /// </summary>
    /// <param name="timestamp">UTC Time in Seconds.</param>
    /// <returns></returns>
    [Alias("TickAsync")]
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

    //  Login

    /// <summary>
    ///     Processes a login message received from a client.
    /// </summary>
    /// <param name="loginMessage">The login message containing authentication data.</param>
    /// <param name="isNewAccount">True if this is a new account registration attempt, false for existing accounts.</param>
    /// <param name="isLoginRetry">True if this is a login retry attempt, false for initial login.</param>
    /// <returns>
    ///     true if login message needs to resend, true if client needs to disconnect.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method handles both new and existing account login flows.
    ///         The <see cref="AliasAttribute" /> ensures backward compatibility with older clients.
    ///     </para>
    /// </remarks>
    [Alias("LoginMsgRcv")]
    public Task<(bool, bool)> ReceiveLoginMessageAsync(LoginMessageStruct loginMessage, bool isNewAccount,
        bool isLoginRetry);

    /// <summary>
    ///     Retrieves the last active time of the player.
    /// </summary>
    /// <returns></returns>
    [Alias("GetLAT")]
    [ReadOnly]
    public ValueTask<DateTimeOffset> GetLastActiveTime();

    /// <summary>
    ///     Disconnects the current player asynchronously.
    /// </summary>
    /// <param name="reason">The reason for DisconnectedMessage.</param>
    /// <returns>
    ///     A task that represents the asynchronous disconnect operation.
    ///     The task result contains a boolean indicating whether the disconnection was successful.
    /// </returns>
    [Alias("DisconnectPlayer")]
    public ValueTask<bool> DisconnectPlayerAsync(int? reason);

    /// <summary>
    ///     Disconnects the current player with messages asynchronously.
    /// </summary>
    /// <param name="messages">An array of messages to be sent to the player before disconnecting.</param>
    /// <returns>
    ///     A task that represents the asynchronous disconnect operation.
    ///     The task result contains a boolean indicating whether the disconnection was successful.
    /// </returns>
    [Alias("DisconnectPlayerWithMsgs")]
    public ValueTask<bool> DisconnectPlayerWithMessagesAsync(PiranhaMessageStruct[] messages);

    /// <summary>
    ///     Sends a LoginOkMessage to the player.
    /// </summary>
    /// <returns></returns>
    [Alias("LoginOk")]
    public Task<bool> LoginOkAsync();

    /// <summary>
    ///     Retrieves the player's localization global ID.
    /// </summary>
    [Alias("GetPlrLocalznGID")]
    public ValueTask<int> GetPlayerLocalizationGlobalId();

    /// <summary>
    ///     Retrieves the player's device language.
    /// </summary>
    [Alias("GetPlrDeviceLang")]
    public ValueTask<string> GetPlayerDeviceLanguage();

    /// <summary>
    ///     Locks the account with the provided unlock code.
    /// </summary>
    [Alias("LockAccount")]
    [AlwaysInterleave]
    public ValueTask<int> LockAccountAsync(string unlockCode);

    /// <summary>
    ///     Bans the account with the provided reason and ban end time.
    /// </summary>
    [Alias("BanAccount")]
    [AlwaysInterleave]
    public ValueTask BanAccountAsync(string reason, DateTime banEndTime);

    /// <summary>
    ///     Unbans and unlocks the account.
    /// </summary>
    [Alias("UnbanAndUnlockAccount")]
    [AlwaysInterleave]
    public ValueTask UnbanAndUnlockAccount();

    [Alias("GBanInfo")]
    [ReadOnly]
    public ValueTask<(bool, DateTime, string)> GetBanInfo();

    [Alias("GLockInfo")]
    [ReadOnly]
    public ValueTask<(bool, string)> GetLockInfo();

    [Alias("GSessionsCount")]
    [ReadOnly]
    public ValueTask<int> GetSessionsCount();

    [Alias("GAccountCreatedTime")]
    [ReadOnly]
    public ValueTask<DateTime> GetAccountCreatedTime();

    [Alias("GPlayTimeSecond")]
    [ReadOnly]
    public ValueTask<ulong> GetPlayTimeSeconds();

    [Alias("GDevicesInfo")]
    [ReadOnly]
    public ValueTask<Dictionary<string, int>> GetDevicesInfo();

    [Alias("GServersInfo")]
    [ReadOnly]
    public ValueTask<Dictionary<string, int>> GetServersInfo();

    [Alias("GClientsInfo")]
    [ReadOnly]
    public ValueTask<Dictionary<string, int>> GetClientsInfo();

    [Alias("CPassToken")]
    public ValueTask<bool> CreatePassToken(string passToken);

    [Alias("VPassToken")]
    public ValueTask<bool> VerifyPassToken(string passToken);
}