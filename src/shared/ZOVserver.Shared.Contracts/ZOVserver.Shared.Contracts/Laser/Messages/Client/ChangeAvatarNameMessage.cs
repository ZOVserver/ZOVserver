using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Messages.Client;

[LaserSerializable]
public partial class ChangeAvatarNameMessage : PiranhaMessage
{
    [Field(0)] public string Name { get; set; } = "Brawler";

    [Field(1)] public bool NameSetByUser { get; set; }

    public override int GetMessageType()
    {
        return 10212;
    }

    public override int GetServiceNodeType()
    {
        return 9;
    }
}