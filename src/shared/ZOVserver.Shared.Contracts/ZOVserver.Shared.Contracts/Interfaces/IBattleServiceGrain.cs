using Orleans;
using Orleans.Concurrency;
using ZOVserver.Shared.Contracts.Structs;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.IBattleServiceGrain")]
public interface IBattleServiceGrain : IGrainWithGuidKey
{
    [Alias("CreateBattle")]
    public Task<int> CreateBattleAsync(int gameModeVar, int locationGlobalId, int[] eventModifiers, int difficulty,
        bool isFriendlyGame,
        Dictionary<long, long> players, Dictionary<long, int> playersFriendlyTeam);

    [Alias("IsBattleActiveWithPlayers")]
    [ReadOnly]
    [ResponseTimeout("00:00:05")]
    public ValueTask<(bool, bool, int)> IsBattleActiveWithPlayersAsync(List<long> players);

    [Alias("GetMyPlayerLoadingData")]
    [ReadOnly]
    [ResponseTimeout("00:00:05")]
    public ValueTask<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
            PiranhaMessageStruct? startLoadingMessage)?>
        GetMyPlayerLoadingData(long accountId, bool showGameHints, byte b);

    [Alias("GetMySpectatorLoadingData")]
    [ReadOnly]
    [ResponseTimeout("00:00:05")]
    public ValueTask<(string ip, int port, ulong lowSessionId, ushort highSessionId, byte[] kanan,
            PiranhaMessageStruct? startLoadingMessage)?>
        GetMySpectatorLoadingData(long accountId, byte b);

    [Alias("AddSpectator")]
    [ResponseTimeout("00:00:08")]
    public ValueTask<int> AddSpectator(long playerAccountId, long accountId, bool brawlTv);

    [Alias("RemoveSpectator")]
    [ResponseTimeout("00:00:08")]
    public ValueTask<bool> RemoveSpectator(long accountId);
}