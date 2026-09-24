using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object.Component;

public class LogicSkillServer(LogicSkillData skillData)
{
    public void Encode(ref BitStream b, int encodeForIndex)
    {
        b.WritePositiveVIntMax255OftenZero(0);
        b.WriteBoolean(false);
        b.WritePositiveVIntMax255OftenZero(0);

        if (skillData.MaxCharge >= 1)
            b.WritePositiveIntMax4095(4000);

        if (skillData.SkillCanChange)
            b.WritePositiveIntMax255((uint)skillData.InstanceId);
    }
}