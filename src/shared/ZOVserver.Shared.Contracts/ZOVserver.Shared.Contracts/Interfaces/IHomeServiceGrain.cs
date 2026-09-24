using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Events;
using ZOVserver.Shared.Contracts.Laser.Combined.Alliance;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Combined.Friend;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.Contracts.Laser.Combined.Team;
using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IHomeServiceGrain")]
public interface IHomeServiceGrain : IGrainWithStringKey
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
    ///     The result indicates whether the messages were successfully sent (true) or not (false).
    /// </returns>
    [Alias("SendMsgsToPlr")]
    public ValueTask<bool> SendMessagesToPlayerAsync(PiranhaMessageStruct[] messages);

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

    //  Home

#pragma warning disable S2368
    [Alias("AddNotifications")]
    public ValueTask<bool> AddNotifications(byte[][] datas);
#pragma warning restore S2368

    [Alias("GetNowTrophies")]
    [ReadOnly]
    public Task<int> GetNowTrophies();

    [Alias("GetAllianceDetailedHomeModel")]
    [AlwaysInterleave]
    public ValueTask<DetailedAllianceMemberHomeModel> GetAllianceDetailedHomeModel();

    [Alias("GetAllianceIdAndAccountIdAndLastAllianceIdChangeTime")]
    [ReadOnly]
    public Task<(long, long, DateTime)> GetAllianceIdAndAccountIdAndLastAllianceIdChangeTime();

    [Alias("GetAllianceIdAndAvatarName")]
    [ReadOnly]
    public Task<(long, string)> GetAllianceIdAndAvatarName();

    [Alias("GetProfileData")]
    [ReadOnly]
    public Task<(HeroEntry[], ProfileStatEntry[], PlayerDisplayData)> GetProfileData();

    [Alias("CorrectAllianceId")]
    public Task CorrectAllianceId(long newId, AllianceParams? allianceParams = null, AllianceRole? role = null,
        int? membersCount = null, int? nowTrophies = null, int? onlineMembers = null);

    [Alias("SetAllianceIdAndGetAllianceDetailedHomeModel")]
    public Task<(bool, DetailedAllianceMemberHomeModel?)> SetAllianceIdAndGetAllianceDetailedHomeModel(long id);

    [Alias("KickFromAlliance")]
    public Task KickFromAlliance();

    [Alias("SendOHD")]
    public Task SendOwnHomeDataAsync(bool first = true);

    [Alias("SendMyAlliance")]
    public Task SendMyAllianceAsync();

    [Alias("GetTeamId")]
    public Task<long> GetTeamId();

    [Alias("GetIsPossibleToTeamInviteAndAvatarName")]
    public Task<(int, string)> GetIsPossibleToTeamInviteAndAvatarName(long teamId, long inviterId,
        long inviterAllianceId);

    [Alias("GetIsPossibleToTeamRequest")]
    public Task<int> GetIsPossibleToTeamRequest(long requesterId, long requesterAllianceId);

    [Alias("TeamInviteAsync")]
    public Task<int> TeamInviteAsync(long teamId, FriendEntry inviter);

    [Alias("TeamInviteCancelAsync")]
    public Task<int> TeamInviteCancelAsync(long teamId);

    [Alias("GetBasicTeamMemberData")]
    public Task<TeamMemberData?> GetBasicTeamMemberData();

    [Alias("RemoveTeamRequest")]
    public Task RemoveTeamRequest(long teamId, int status);

    [Alias("KickFromTeam")]
    [OneWay]
    public Task KickFromTeamAsync(long teamId);

    [Alias("GetFriendEntry")]
    [ReadOnly]
    public ValueTask<FriendEntry> GetFriendEntryAsync();

    [Alias("GetFriendOnlineStatusEntry")]
    [ReadOnly]
    public ValueTask<FriendOnlineStatusEntry?> GetFriendOnlineStatusEntryAsync();

    [Alias("SyncWithHome")]
    [ReadOnly]
    public ValueTask<(FriendEntry, FriendOnlineStatusEntry?)> SyncFriendshipWithHomeAsync();

    [Alias("UpdateMyFriendEntryInFriendship")]
    [OneWay]
    public Task UpdateMyFriendEntryInFriendshipAsync();

    [Alias("UpdateMyFriendOnlineStatusEntryInFriendship")]
    [OneWay]
    public Task UpdateMyFriendOnlineStatusEntryInFriendshipAsync();

    [Alias("UpdateAllianceTeamEntryInFriendship")]
    [OneWay]
    public Task UpdateAllianceTeamEntryInFriendshipAsync(AllianceTeamEntry? allianceTeamEntry);

    [Alias("GetHomeCreatedTime")]
    [ReadOnly]
    public ValueTask<DateTime> GetHomeCreatedTime();

    [Alias("BattleEnd")]
    public Task<bool> BattleEndAsync(int brawlerId, int eventId, bool win, int trophiesResult,
        int experienceResult,
        int miniBoxTokensResult, int starPointsResult, int winType, int winData);

    [Alias("GetMyBrawlerRankingData")]
    [ReadOnly]
    public Task<PlayerBrawlerRankingData?> GetMyBrawlerRankingDataAsync(int characterId);

    [Alias("GetMyRankingData")]
    [ReadOnly]
    public Task<PlayerRankingData?> GetMyRankingDataAsync();

    [Alias("StopPlayersSearch")]
    [OneWay]
    public Task StopPlayersSearchAsync();

    [Alias("SendMatchmakeStatus")]
    [OneWay]
    public Task SendMatchmakeStatusAsync(Guid id, Dictionary<long, long> players, int maxPlayers, int fs);

    [Alias("KickFromMatchmaking")]
    [OneWay]
    public Task KickFromMatchmakingAsync(Guid id, int reason);

    [Alias("ToBattleFromMatchmaking")]
    [OneWay]
    public Task ToBattleFromMatchmakingAsync(Guid id);

    [Alias("SetBattle")]
    [ResponseTimeout("00:00:05")]
    public ValueTask<LogicPlayer?> SetBattleAsync(Guid battleServiceGrainKey);

    [Alias("GetMyBattleServiceGrainId")]
    [ReadOnly]
    public ValueTask<Guid?> GetMyBattleServiceGrainId();

    [OneWay]
    [Alias("SendBattleServerError")]
    public Task SendBattleServerErrorAsync(int error, bool writeState = true);

    [OneWay]
    [Alias("InitiateBattleEntry")]
    public Task InitiateBattleEntryAsync();

    [Alias("SetB")]
    public ValueTask SetB(byte b);

    [Alias("SaveHomeState")]
    public ValueTask SaveHomeStateAsync();
}