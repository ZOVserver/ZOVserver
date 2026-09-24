using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Server;

public class SeasonRewardsMessage : PiranhaMessage
{
    public static readonly int[] TrophiesStart =
    [
        550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400
    ];

    public static readonly int[] TrophiesEnd =
    [
        599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, -1
    ];

    public static readonly int[] StarPointsSeasonRewardAmount =
    [
        70, 120, 160, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 440, 460, 480
    ];

    public static readonly int[] TrophiesInReset =
    [
        525, 575, 625, 650, 700, 750, 775, 825, 875, 900, 925, 950, 975, 1000, 1025, 1050, 1075, 1100
    ];

    public override void CustomEncode(ByteStream stream)
    {
        stream.WriteVInt32(1);
        stream.WriteVInt32(TrophiesStart.Length);

        for (var i = 0; i < TrophiesStart.Length; i++)
        {
            stream.WriteVInt32(TrophiesStart[i]);
            stream.WriteVInt32(TrophiesEnd[i]);

            stream.WriteVInt32(StarPointsSeasonRewardAmount[i]);
            stream.WriteVInt32(TrophiesInReset[i]);

            stream.WriteVInt32(0);
            stream.WriteBoolean(false);
        }
    }

    public override void CustomDecode(ByteStream stream)
    {
        _ = stream.ReadVInt32();

        var count = stream.ReadVInt32();

        for (var i = 0; i < count; i++)
        {
            var start = stream.ReadVInt32();
            var end = stream.ReadVInt32();
            var amount = stream.ReadVInt32();
            var reset = stream.ReadVInt32();
            var unknown = stream.ReadVInt32();
            var unknown2 = stream.ReadBoolean();
        }
    }

    public override int GetMessageType()
    {
        return 24123;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}