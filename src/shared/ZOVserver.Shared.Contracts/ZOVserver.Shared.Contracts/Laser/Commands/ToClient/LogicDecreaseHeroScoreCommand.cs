using ZOVserver.Shared.Contracts.Generator;
using ZOVserver.Shared.Contracts.Laser.Combined.Entries;

namespace ZOVserver.Shared.Contracts.Laser.Commands.ToClient;

[LaserSerializable]
public partial class LogicDecreaseHeroScoreCommand : LogicServerCommand
{
    [Field(0)] public int AllTrophiesNow { get; set; }

    [Field(1, BaseMethodAfter = true)] public List<ScoreChange> ScoreChanges { get; set; } = [];

    public override int GetCommandType()
    {
        return 205;
    }
}