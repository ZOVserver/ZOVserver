using ZOVserver.Shared.Contracts.Generator;

namespace ZOVserver.Shared.Contracts.Laser.Commands.FromClient;

[LaserSerializable]
public partial class LogicSetPlayerThumbnailCommand : LogicCommand
{
    [Field(0, BaseMethodBefore = true)] public int ThumbnailGlobalId { get; set; }

    public override int GetCommandType()
    {
        return 505;
    }
}