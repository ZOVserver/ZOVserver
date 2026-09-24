using MessagePack;
using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;
using ZOVserver.Shared.Contracts.Laser.Machine;

namespace ZOVserver.Shared.Contracts.Laser.Combined.Home.NextA.NextB;

[MessagePackObject]
[LaserSerializable]
public partial class LogicOfferBundles : LaserContract
{
    [Key(0)] [Field(10)] public ChronosTextEntry OfferHeader { get; set; } = null!;

    [Key(1)] public Guid CustomId { get; set; }

    [Key(2)] [Field(2, IsVarInt = true)] public int OfferPrice { get; set; }

    [Key(3)] [Field(9, IsVarInt = true)] public int OfferOldPrice { get; set; }

    [Key(4)] [Field(4, IsVarInt = true)] public int State { get; set; }

    [Key(5)] [Field(11)] public bool ConfirmPurchase { get; set; }

    [Key(6)] [Field(8)] public bool IsDaily { get; set; }

    [Key(7)] [Field(12)] public string BackgroundTheme { get; set; } = string.Empty;

    [Key(8)]
    [Field(3, IsVarInt = true, CalculateSecondsLeft = true)]
    public DateTime EndTime { get; set; }

    [Key(9)]
    [Field(0, UseCustomContract = true)]
    public List<LogicGemOffer> LogicGemOffers { get; set; } = [];

    [Key(10)] [Field(1, IsVarInt = true)] public int ShopPriceType { get; set; }

    [Key(11)] [Field(6)] public bool Purchased { get; set; }

    [Key(12)] [Field(5, IsVarInt = true)] public int Unk28 { get; set; }

    [Key(13)] [Field(7, IsVarInt = true)] public int Unk36 { get; set; }
}