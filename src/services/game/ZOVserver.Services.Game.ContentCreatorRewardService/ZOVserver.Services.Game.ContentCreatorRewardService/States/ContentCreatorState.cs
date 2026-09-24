using MessagePack;

namespace ZOVserver.Services.Game.ContentCreatorRewardService.States;

[MessagePackObject]
public class ContentCreatorState
{
    [Key(3)] public long AlphaDiamondsReward;

    [Key(0)] public bool Activated { get; set; }

    [Key(1)] public long ContentCreatorAccountId { get; set; }

    [Key(2)] public HashSet<long> Supporters { get; set; } = [];

    [Key(4)] public List<(DateTime, int)> AccruedRewards { get; set; } = [];

    [Key(5)] public int ContentCreatorLevel { get; set; }
}