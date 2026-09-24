using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class BattleEndMessage : PiranhaMessage
{
    public override void CustomEncode(ByteStream stream)
    {
        base.CustomEncode(stream);

        stream.WriteVInt32(0); // game mode, 0 = 3vs3, 2 = showdown, 3 = robo rumble, 4 = big game, 5 = duo showdown, 6 = boss fight
        stream.WriteVInt32(0); // result
        stream.WriteVInt32(777); // tokens gained
        stream.WriteVInt32(2); // trophies result
        stream.WriteVInt32(0);

        stream.WriteVInt32(444); // tokens (doubled)
        stream.WriteVInt32(333); // +tokens (double tokens event)
        stream.WriteVInt32(1000); // tokens doublers remained

        stream.WriteVInt32(2); // lvl or seconds
        stream.WriteVInt32(0);
        stream.WriteVInt32(0);
        stream.WriteVInt32(0);
        stream.WriteVInt32(0);
        stream.WriteVInt32(0);

        #region sub_5E55FC

        stream.WriteVInt32(0);

        stream.WriteBoolean(false); // star token
        stream.WriteBoolean(false); // no experience left
        stream.WriteBoolean(false); // no tokens left
        stream.WriteBoolean(false); // skip battle end
        stream.WriteBoolean(true); // trophies battle end
        stream.WriteBoolean(false); // skip battle end
        stream.WriteBoolean(false); // power league battle end
        stream.WriteBoolean(false); // trophies battle end
        stream.WriteBoolean(false); // trophies battle end
        stream.WriteBoolean(false); // win with selected brawler

        var v1 = stream.WriteVInt32(3);
        for (var i = 0; i < v1; i++)
        {
            // PlayerEntry start

            stream.WriteBoolean(i == 0); // isOwnPlayer
            stream.WriteBoolean(false); // isEnemy
            stream.WriteBoolean(i == 0); // isStarPlayer

            ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(16, i)); // brawler id
            ByteStreamHelper.WriteDataReference(stream, 0); // skin id

            stream.WriteVInt32(105); // brawler trophies
            stream.WriteVInt32(106); // power league trophies
            stream.WriteVInt32(2); // powerLevel

            if (stream.WriteBoolean(i == 0)) // isOwn
                stream.WriteI64(i + 1); // accountId

            new PlayerDisplayData
                {
                    AvatarName = "Test " + i, NameColor = GlobalId.CreateGlobalId(43, 0),
                    Thumbnail = GlobalId.CreateGlobalId(28, 0)
                }
                .Encode(stream);

            // PlayerEntry end
        }

        var v2 = stream.WriteVInt32(1);
        {
            // XpEntry start

            stream.WriteVInt32(0);
            stream.WriteVInt32(1);

            /*stream.WriteVInt32(8);
            stream.WriteVInt32(1);*/ // star player experience

            // XpEntry end
        }

        var v3 = stream.WriteVInt32(0);
        for (var i = 0; i < v3; i++) ByteStreamHelper.WriteDataReference(stream, 0);

        var v4 = stream.WriteVInt32(2);
        {
            // LogicMilestoneProgress start

            stream.WriteVInt32(1); // unk
            stream.WriteVInt32(223); // brawler trophies
            stream.WriteVInt32(1000); // max brawler trophies

            stream.WriteVInt32(5); // unk
            stream.WriteVInt32(45); // now experience (>= max)
            stream.WriteVInt32(45); // experience (before the battle)

            // LogicMilestoneProgress end
        }

        ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(28, 0));

        if (stream.WriteBoolean(true)) // PlayAgainStatus
        {
            stream.WriteI32(0);

            // sub_D867C
            var v5 = stream.WriteVInt32(3);
            for (var i = 0; i < v5; i++) stream.WriteI64(i);

            var v6 = stream.WriteVInt32(3);
            for (var i = 0; i < v6; i++) stream.WriteI64(i);

            stream.WriteI32(0);
            stream.WriteI32(0);
        }

        #endregion
    }

    public override void CustomDecode(ByteStream stream)
    {
        base.CustomDecode(stream);

        var gameMode =
            stream.ReadVInt32(); // 0 = 3vs3, 2 = showdown, 3 = robo rumble, 4 = big game, 5 = duo showdown, 6 = boss fight
        var result = stream.ReadVInt32();
        var tokensGained = stream.ReadVInt32(); // 777
        var trophiesResult = stream.ReadVInt32(); // 2
        var unknown1 = stream.ReadVInt32();

        var tokensDoubled = stream.ReadVInt32(); // 444
        var tokensDoubleEvent = stream.ReadVInt32(); // 333
        var tokensDoublersRemained = stream.ReadVInt32(); // 1000

        var levelOrSeconds = stream.ReadVInt32(); // 2
        var unknown2 = stream.ReadVInt32();
        var unknown3 = stream.ReadVInt32();
        var unknown4 = stream.ReadVInt32();
        var unknown5 = stream.ReadVInt32();
        var unknown6 = stream.ReadVInt32();

        var unknown7 = stream.ReadVInt32();

        var starToken = stream.ReadBoolean();
        var noExperienceLeft = stream.ReadBoolean();
        var noTokensLeft = stream.ReadBoolean();
        var skipBattleEnd1 = stream.ReadBoolean();
        var trophiesBattleEnd1 = stream.ReadBoolean();
        var skipBattleEnd2 = stream.ReadBoolean();
        var powerLeagueBattleEnd = stream.ReadBoolean();
        var trophiesBattleEnd2 = stream.ReadBoolean();
        var trophiesBattleEnd3 = stream.ReadBoolean();
        var winWithSelectedBrawler = stream.ReadBoolean();

        var playerCount = stream.ReadVInt32(); // 3
        for (var i = 0; i < playerCount; i++)
        {
            var isOwnPlayer = stream.ReadBoolean();
            var isEnemy = stream.ReadBoolean();
            var isStarPlayer = stream.ReadBoolean();

            long brawlerId = ByteStreamHelper.ReadDataReference(stream); // GlobalId.CreateGlobalId(16, i)
            long skinId = ByteStreamHelper.ReadDataReference(stream);

            var brawlerTrophies = stream.ReadVInt32(); // 105
            var powerLeagueTrophies = stream.ReadVInt32(); // 106
            var powerLevel = stream.ReadVInt32(); // 2

            var hasAccountId = stream.ReadBoolean();
            long accountId = 0;
            if (hasAccountId) accountId = stream.ReadI64(); // i + 1

            new PlayerDisplayData().Decode(stream);
        }

        var xpEntryCount = stream.ReadVInt32(); // 1
        for (var i = 0; i < xpEntryCount; i++)
        {
            var xpValue1 = stream.ReadVInt32();
            var xpValue2 = stream.ReadVInt32();
        }

        var referenceCount = stream.ReadVInt32(); // 0
        for (var i = 0; i < referenceCount; i++)
        {
            long reference = ByteStreamHelper.ReadDataReference(stream);
        }

        var milestoneCount = stream.ReadVInt32(); // 2
        for (var i = 0; i < milestoneCount; i++)
        {
            var milestoneUnknown1 = stream.ReadVInt32(); // 1
            var milestoneBrawlerTrophies = stream.ReadVInt32(); // 223
            var milestoneMaxBrawlerTrophies = stream.ReadVInt32(); // 1000

            var milestoneUnknown2 = stream.ReadVInt32(); // 5
            var milestoneCurrentExperience = stream.ReadVInt32(); // 45
            var milestonePreviousExperience = stream.ReadVInt32(); // 45
        }

        long thumbnailReference = ByteStreamHelper.ReadDataReference(stream); // GlobalId.CreateGlobalId(28, 0)

        var hasPlayAgainStatus = stream.ReadBoolean();
        if (hasPlayAgainStatus)
        {
            var playAgainValue1 = stream.ReadI32();

            var playAgainCount1 = stream.ReadVInt32(); // 3
            for (var i = 0; i < playAgainCount1; i++)
            {
                var id1 = stream.ReadI64();
            }

            var playAgainCount2 = stream.ReadVInt32(); // 3
            for (var i = 0; i < playAgainCount2; i++)
            {
                var id2 = stream.ReadI64();
            }

            var playAgainValue2 = stream.ReadI32();
            var playAgainValue3 = stream.ReadI32();
        }
    }

    public override int GetMessageType()
    {
        return 23456;
    }

    public override int GetServiceNodeType()
    {
        return 27;
    }
}