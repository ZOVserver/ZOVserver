using Orleans;
using Orleans.Concurrency;

namespace ZOVserver.Shared.Contracts.Interfaces;

[Alias("ZOVserver.Shared.Contracts.Interfaces.ICreatorRewardServiceGrain")]
public interface IContentCreatorRewardServiceGrain : IGrainWithStringKey
{
    [Alias("Activate")]
    public ValueTask Activate();

    [Alias("Deactivate")]
    public ValueTask Deactivate();

    [Alias("IsActivated")]
    [ReadOnly]
    public ValueTask<bool> IsActivated();

    [Alias("ChangeCCAccountId")]
    public ValueTask ChangeContentCreatorAccountId(long accountId);

    [Alias("GetCCAccountId")]
    [ReadOnly]
    public ValueTask<long> GetContentCreatorAccountId();

    [Alias("AddSupporter")]
    public ValueTask AddSupporter(long supporterId);

    [Alias("RemoveSupporter")]
    public ValueTask RemoveSupporter(long supporterId);

    [Alias("GetSupportersCount")]
    [ReadOnly]
    public ValueTask<int> GetSupportersCount();

    [Alias("AddSupportedDiamondsReward")]
    [AlwaysInterleave]
    public ValueTask AddSupportedDiamondsReward(int diamonds);

    [Alias("GetCCAccruedRewards")]
    [ReadOnly]
    public ValueTask<List<(DateTime, int)>> GetContentCreatorAccruedRewards();

    [Alias("GetContentCreatorLevel")]
    [ReadOnly]
    public ValueTask<int> GetContentCreatorLevel();

    [Alias("SetContentCreatorLevel")]
    public ValueTask SetContentCreatorLevel(int level);
}