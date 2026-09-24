using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;

public interface IGameObject
{
    public int Id { get; set; }

    public bool ShouldRemove { get; set; }

    public LogicData ObjectData { get; set; }
    public GameObjectTypeHelperTable ObjectType { get; set; }
    public bool IsDataInitialized { get; set; }

    public void OnAdd(LogicBattleModeServer battleMode);
    public void OnRemove();

    public void Tick();

    public bool ShouldEncodeFor(int encodeForIndex);
    public void Encode(ref BitStream b, int encodeForIndex);

    public void Decode(ref BitStream b, int decodeForIndex)
    {
    }
}