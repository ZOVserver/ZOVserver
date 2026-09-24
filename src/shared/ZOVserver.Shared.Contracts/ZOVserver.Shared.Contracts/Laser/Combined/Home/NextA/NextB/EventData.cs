using MessagePack;
using ZOVserver.Shared.Contracts.Laser.Machine;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
public class EventData : LaserContract
{
    [Key(0)] public int Id { get; set; }

    [Key(1)] public int Slot { get; set; }

    [Key(2)] public DateTime EndTime { get; set; }

    [Key(3)] public int LocationGlobalId { get; set; }

    [Key(4)] public int MiniBoxReward { get; set; }

    [Key(5)] public bool MiniBoxRewardClaimed { get; set; }

    [Key(6)] public bool FirstWinRewardClaimed { get; set; }

    [Key(8)] public List<int> Modifiers { get; set; } = [];

    [Key(9)] public int TicketEventDifficulty { get; set; }

    [IgnoreMember] public bool IsUpcoming { get; set; }

    public override void CustomEncode(ByteStream byteStream)
    {
        byteStream.WriteVInt32(Id);
        byteStream.WriteVInt32(Slot);

        byteStream.WriteVInt32((int)(IsUpcoming ? (EndTime - DateTime.UtcNow).TotalSeconds + 1 : 0));

        byteStream.WriteVInt32((int)(!IsUpcoming
            ? (EndTime - DateTime.UtcNow).TotalSeconds + 1
            : ((DateTimeOffset)EndTime).ToUnixTimeSeconds()));

        byteStream.WriteVInt32(!IsUpcoming ? MiniBoxReward : 0); // this + 16 
        ByteStreamHelper.WriteDataReference(byteStream, LocationGlobalId); // this + 20
        byteStream.WriteVInt32(!IsUpcoming
            ? !FirstWinRewardClaimed ? !MiniBoxRewardClaimed ? 0 : 2 : 3
            : 3); // this + 24

        byteStream.WriteString(""); // this + 28

        byteStream.WriteVInt32(0); // this + 32
        byteStream.WriteVInt32(0); // this + 36
        byteStream.WriteVInt32(0); // this + 40

        byteStream.WriteVInt32(Modifiers.Count); // this + 44 + 8
        foreach (var modifier in Modifiers)
            byteStream.WriteVInt32(modifier);

        byteStream.WriteVInt32(TicketEventDifficulty); // this + 48   
    }

    public override void CustomDecode(ByteStream byteStream)
    {
        Id = byteStream.ReadVInt32();
        Slot = byteStream.ReadVInt32();

        var timeValue1 = byteStream.ReadVInt32();
        var timeValue2 = byteStream.ReadVInt32();

        IsUpcoming = timeValue1 == 0;

        EndTime = IsUpcoming ? DateTime.UtcNow.AddSeconds(timeValue2 - 1) : DateTime.UtcNow.AddSeconds(timeValue1 - 1);

        MiniBoxReward = byteStream.ReadVInt32();
        LocationGlobalId = ByteStreamHelper.ReadDataReference(byteStream);

        var statusValue = byteStream.ReadVInt32();

        if (!IsUpcoming)
        {
            FirstWinRewardClaimed = statusValue == 3;
            MiniBoxRewardClaimed = statusValue is 2 or 3;
        }
        else
        {
            FirstWinRewardClaimed = false;
            MiniBoxRewardClaimed = false;
        }

        byteStream.ReadString();

        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();

        var modifiersCount = byteStream.ReadVInt32();

        Modifiers = [];
        for (var i = 0; i < modifiersCount; i++)
            Modifiers.Add(byteStream.ReadVInt32());

        TicketEventDifficulty = byteStream.ReadVInt32();
    }
}