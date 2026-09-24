using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Proto;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Streams.Helper;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home;

public class LogicClientAvatar
{
    public long AccountId { get; set; }
    public long HomeId { get; set; }

    public string AvatarName { get; set; } = string.Empty;
    public bool NameSetByUser { get; set; }

    public List<HeroEntry> HeroEntries { get; set; } = [];

    public int MiniBoxTokens { get; set; }
    public int BigBoxStarTokens { get; set; }

    public int Gold { get; set; }
    public int Diamonds { get; set; }

    public int StarPoints { get; set; }

    public int TutorialState { get; set; }

    public void CustomEncode(ByteStream byteStream)
    {
        var cardsDict = new Dictionary<int, bool>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var hero in HeroEntries)
        foreach (var kvp in hero.StarPowersContainer)
            cardsDict.TryAdd(kvp.Key, kvp.Value);

        ByteStreamHelper.EncodeLogicLong(byteStream, AccountId); // this + 16
        ByteStreamHelper.EncodeLogicLong(byteStream, AccountId); // this + 24
        ByteStreamHelper.EncodeLogicLong(byteStream, HomeId); // this + 32

        byteStream.WriteString(AvatarName); // this + 40
        byteStream.WriteBoolean(NameSetByUser); // this + 140
        byteStream.WriteI32(-1); // this + 144

        byteStream.WriteVInt32(8); // constant value: 8

        byteStream.WriteVInt32(HeroEntries.Count + 4);
        {
            foreach (var hero in HeroEntries)
                new LogicDataSlot { DataGlobalId = hero.CardUnlockGlobalId, Count = 1 }.Encode(byteStream);

            new LogicDataSlot
                { DataGlobalId = GlobalId.CreateGlobalId(5, 1), Count = MiniBoxTokens }.Encode(byteStream);
            new LogicDataSlot
                { DataGlobalId = GlobalId.CreateGlobalId(5, 9), Count = BigBoxStarTokens }.Encode(byteStream);

            new LogicDataSlot { DataGlobalId = GlobalId.CreateGlobalId(5, 8), Count = Gold }.Encode(byteStream);
            new LogicDataSlot { DataGlobalId = GlobalId.CreateGlobalId(5, 10), Count = StarPoints }.Encode(byteStream);
        }

        byteStream.WriteVInt32(HeroEntries.Count);
        foreach (var hero in HeroEntries)
            new LogicDataSlot { DataGlobalId = hero.CharacterGlobalId, Count = hero.Trophies }.Encode(byteStream);

        byteStream.WriteVInt32(HeroEntries.Count);
        foreach (var hero in HeroEntries)
            new LogicDataSlot { DataGlobalId = hero.CharacterGlobalId, Count = hero.MaxTrophies }.Encode(byteStream);

        byteStream.WriteVInt32(0);

        byteStream.WriteVInt32(HeroEntries.Count);
        foreach (var hero in HeroEntries)
            new LogicDataSlot { DataGlobalId = hero.CharacterGlobalId, Count = hero.PowerPoints }.Encode(byteStream);

        byteStream.WriteVInt32(HeroEntries.Count);
        foreach (var hero in HeroEntries)
            new LogicDataSlot { DataGlobalId = hero.CharacterGlobalId, Count = hero.PowerLevel - 1 }.Encode(byteStream);

        byteStream.WriteVInt32(cardsDict.Count);
        foreach (var card in cardsDict)
            new LogicDataSlot { DataGlobalId = card.Key, Count = card.Value ? 2 : 1 }.Encode(byteStream);

        byteStream.WriteVInt32(HeroEntries.Count);
        foreach (var hero in HeroEntries)
            new LogicDataSlot { DataGlobalId = hero.CharacterGlobalId, Count = hero.CharacterState }.Encode(byteStream);

        byteStream.WriteVInt32(Diamonds); // this + 100 // diamonds

        byteStream.WriteVInt32(0); // this + 104
        byteStream.WriteVInt32(0); // this + 92
        byteStream.WriteVInt32(0); // this + 96
        byteStream.WriteVInt32(0); // this + 108
        byteStream.WriteVInt32(0); // this + 116
        byteStream.WriteVInt32(0); // this + 120
        byteStream.WriteVInt32(0); // this + 124
        byteStream.WriteVInt32(0); // this + 128
        byteStream.WriteVInt32(0); // this + 132
        byteStream.WriteVInt32(0); // this + 136

        byteStream.WriteVInt32(TutorialState); // this + 148 // tutorial state
    }

    public void CustomDecode(ByteStream byteStream)
    {
        AccountId = ByteStreamHelper.DecodeLogicLong(byteStream); // this + 16
        _ = ByteStreamHelper.DecodeLogicLong(byteStream); // this + 24
        HomeId = ByteStreamHelper.DecodeLogicLong(byteStream); // this + 32

        AvatarName = byteStream.ReadString(); // this + 40
        NameSetByUser = byteStream.ReadBoolean(); // this + 140
        _ = byteStream.ReadI32(); // this + 144

        _ = byteStream.ReadVInt32();

        var slotCount1 = byteStream.ReadVInt32();
        HeroEntries = new List<HeroEntry>();

        for (var i = 0; i < slotCount1; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            if (GlobalId.GetClassId(slot.DataGlobalId) == 16)
                HeroEntries.Add(new HeroEntry
                {
                    CardUnlockGlobalId = slot.DataGlobalId
                });
            else if (GlobalId.GetClassId(slot.DataGlobalId) == 5)
                switch (GlobalId.GetInstanceId(slot.DataGlobalId))
                {
                    case 1:
                        MiniBoxTokens = slot.Count;
                        break;
                    case 9:
                        BigBoxStarTokens = slot.Count;
                        break;
                    case 8:
                        Gold = slot.Count;
                        break;
                    case 10:
                        StarPoints = slot.Count;
                        break;
                }
        }

        var trophyCount = byteStream.ReadVInt32();
        for (var i = 0; i < trophyCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            var hero = HeroEntries.FirstOrDefault(h => h.CharacterGlobalId == slot.DataGlobalId);
            if (hero != null) hero.Trophies = slot.Count;
        }

        var maxTrophyCount = byteStream.ReadVInt32();
        for (var i = 0; i < maxTrophyCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            var hero = HeroEntries.FirstOrDefault(h => h.CharacterGlobalId == slot.DataGlobalId);
            if (hero != null) hero.MaxTrophies = slot.Count;
        }

        _ = byteStream.ReadVInt32(); // 0

        var powerPointsCount = byteStream.ReadVInt32();
        for (var i = 0; i < powerPointsCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            var hero = HeroEntries.FirstOrDefault(h => h.CharacterGlobalId == slot.DataGlobalId);
            if (hero != null) hero.PowerPoints = slot.Count;
        }

        var powerLevelCount = byteStream.ReadVInt32();
        for (var i = 0; i < powerLevelCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            var hero = HeroEntries.FirstOrDefault(h => h.CharacterGlobalId == slot.DataGlobalId);
            if (hero != null) hero.PowerLevel = slot.Count + 1;
        }

        var starPowersCount = byteStream.ReadVInt32();
        var cardsDict = new Dictionary<int, bool>();

        for (var i = 0; i < starPowersCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);
            cardsDict[slot.DataGlobalId] = slot.Count >= 2;
        }

        foreach (var hero in HeroEntries)
            hero.StarPowersContainer = cardsDict
                .Where(kvp => GlobalId.GetClassId(kvp.Key) == 23)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var characterStateCount = byteStream.ReadVInt32();
        for (var i = 0; i < characterStateCount; i++)
        {
            var slot = new LogicDataSlot();
            slot.Decode(byteStream);

            var hero = HeroEntries.FirstOrDefault(h => h.CharacterGlobalId == slot.DataGlobalId);
            if (hero != null) hero.CharacterState = slot.Count;
        }

        Diamonds = byteStream.ReadVInt32();

        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();
        byteStream.ReadVInt32();

        TutorialState = byteStream.ReadVInt32(); // this + 148
    }
}