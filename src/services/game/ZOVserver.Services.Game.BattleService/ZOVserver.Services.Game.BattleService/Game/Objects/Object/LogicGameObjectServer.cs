using System.Numerics;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Shared.TitanRemnants.Helper.Battle;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object;

public struct LogicGameObjectServer : IGameObject
{
    public int Id { get; set; }
    public bool ShouldRemove { get; set; }

    public LogicBattleModeServer? BattleMode { get; private set; }

    public void OnAdd(LogicBattleModeServer battleMode)
    {
        FadeCounter = 10;

        BattleMode = battleMode;
    }

    public void OnRemove()
    {
        Position = default;
        Index = 0;
        FadeCounter = 0;
        BattleMode = null;
    }

    public void Tick()
    {
    }

    public bool ShouldEncodeFor(int encodeForIndex)
    {
        return FadeCounter >= 1 || Index / 16 == encodeForIndex / 16 || encodeForIndex < 0;
    }

    public LogicData ObjectData { get; set; }
    public GameObjectTypeHelperTable ObjectType { get; set; }
    public bool IsDataInitialized { get; set; }

    public Vector3 Position;

    public uint Index { get; set; }

    public uint FadeCounter { get; set; }

    /// <summary>
    ///     Initializes object data.
    /// </summary>
    /// <returns>False if data is invalid and object cannot be added.</returns>
    public bool InitObjectData(int globalId)
    {
        var logicData = LogicDataTables.GetDataById(globalId);
        return logicData != null && InitObjectData(logicData);
    }

    /// <summary>
    ///     Initializes object data.
    /// </summary>
    /// <returns>False if data is invalid and object cannot be added.</returns>
    public bool InitObjectData(int classId, int instanceId)
    {
        var logicData = LogicDataTables.GetDataById(classId, instanceId);
        return logicData != null && InitObjectData(logicData);
    }

    /// <summary>
    ///     Initializes object data.
    /// </summary>
    /// <returns>False if data is invalid and object cannot be added.</returns>
    public bool InitObjectData(int classId, string name)
    {
        var logicData = LogicDataTables.GetDataByName(classId, name);
        return logicData != null && InitObjectData(logicData);
    }

    /// <summary>
    ///     Initializes object data.
    /// </summary>
    /// <returns>False if data is invalid and object cannot be added.</returns>
    public bool InitObjectData(LogicData data) // boss
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data == null)
            return false;

        var objectType = GlobalIdToObjectTypeConverter.GetObjectType(data.GlobalId);

        if (objectType == null)
            return false;

        ObjectData = data;
        ObjectType = objectType.Value;
        IsDataInitialized = true;

        return true;
    }

    public void Encode(ref BitStream b, int encodeForIndex)
    {
        if (false) // projectile?
        {
            b.WriteIntMax65535((int)MathF.Round(Position.X, MidpointRounding.AwayFromZero));
            b.WriteIntMax65535((int)MathF.Round(Position.Y, MidpointRounding.AwayFromZero));
        }

        b.WritePositiveVIntMax65535((uint)MathF.Round(Position.X, MidpointRounding.AwayFromZero));
        b.WritePositiveVIntMax65535((uint)MathF.Round(Position.Y, MidpointRounding.AwayFromZero));

        b.WritePositiveVIntMax255(Index);

        b.WritePositiveVIntMax65535((uint)MathF.Round(Position.Z, MidpointRounding.AwayFromZero));

        if (ObjectType != GameObjectTypeHelperTable.Projectile)
            b.WritePositiveIntMax15(FadeCounter);
    }
}