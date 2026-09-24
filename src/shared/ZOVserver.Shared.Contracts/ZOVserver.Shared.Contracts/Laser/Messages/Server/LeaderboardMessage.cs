using ZOVserver.Shared.Contracts.Models;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class LeaderboardMessage : PiranhaMessage
{
    public int LeaderboardType { get; set; }
    public int BrawlerGlobalId { get; set; }
    public bool IsRegional { get; set; }
    public string Region { get; set; } = "RU";
    public List<PlayerRankingData> PlayerRankingDatas { get; set; } = [];
    public List<AllianceRankingData> AllianceRankingDatas { get; set; } = [];
    public List<PlayerBrawlerRankingData> PlayerBrawlerRankingDatas { get; set; } = [];
    public int MyTrophies { get; set; }
    public int MyIndex { get; set; }

    public override void CustomEncode(ByteStream stream)
    {
        base.CustomEncode(stream);

        stream.WriteVInt32(LeaderboardType);
        stream.WriteVInt32(0);

        ByteStreamHelper.WriteDataReference(stream, BrawlerGlobalId);

        stream.WriteString(IsRegional ? Region : null);

        switch (LeaderboardType)
        {
            case 1:
            {
                stream.WriteVInt32(PlayerRankingDatas.Count);

                foreach (var rankingData in PlayerRankingDatas)
                {
                    stream.WriteVInt64(rankingData.AccountId);
                    stream.WriteVInt32(1);
                    stream.WriteVInt32(rankingData.Trophies);

                    stream.WriteBoolean(true);
                    stream.WriteString(rankingData.AllianceName);
                    rankingData.DisplayData?.Encode(stream);

                    stream.WriteBoolean(false);
                }

                break;
            }
            case 2:
            {
                stream.WriteVInt32(AllianceRankingDatas.Count);

                foreach (var allianceRankingData in AllianceRankingDatas)
                {
                    stream.WriteVInt64(allianceRankingData.AllianceId);
                    stream.WriteVInt32(1);
                    stream.WriteVInt32(allianceRankingData.Trophies);

                    stream.WriteBoolean(false);

                    stream.WriteBoolean(true);
                    stream.WriteString(allianceRankingData.AllianceName);
                    stream.WriteVInt32(allianceRankingData.MembersCount);
                    ByteStreamHelper.WriteDataReference(stream, allianceRankingData.BadgeGlobalId);
                }

                break;
            }
            default:
            {
                stream.WriteVInt32(PlayerBrawlerRankingDatas.Count);

                foreach (var playerBrawlerRankingData in PlayerBrawlerRankingDatas)
                {
                    stream.WriteVInt64(playerBrawlerRankingData.AccountId);
                    stream.WriteVInt32(1);
                    stream.WriteVInt32(playerBrawlerRankingData.BrawlerTrophies);

                    stream.WriteBoolean(true);
                    stream.WriteString(playerBrawlerRankingData.AllianceName);
                    playerBrawlerRankingData.DisplayData?.Encode(stream);

                    stream.WriteBoolean(false);
                }

                break;
            }
        }

        stream.WriteVInt32(0);
        stream.WriteVInt32(MyIndex);
        stream.WriteVInt32(0);
        stream.WriteVInt32(0);
        stream.WriteString(Region);
    }

    public override int GetMessageType()
    {
        return 24403;
    }

    public override int GetServiceNodeType()
    {
        return 13;
    }
}