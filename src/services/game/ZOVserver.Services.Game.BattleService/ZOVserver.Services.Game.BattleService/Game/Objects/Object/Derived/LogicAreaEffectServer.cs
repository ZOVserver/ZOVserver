using System.Runtime.CompilerServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;

public struct LogicAreaEffectServer : IGameObject
{
    public LogicGameObjectServer BaseObject;

    public int Id
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseObject.Id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseObject.Id = value;
    }

    public bool ShouldRemove
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseObject.ShouldRemove;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseObject.ShouldRemove = value;
    }

    public LogicData ObjectData
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseObject.ObjectData;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseObject.ObjectData = value;
    }

    public GameObjectTypeHelperTable ObjectType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseObject.ObjectType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseObject.ObjectType = value;
    }

    public bool IsDataInitialized
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BaseObject.IsDataInitialized;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BaseObject.IsDataInitialized = value;
    }

    public LogicAreaEffectData AreaEffect { get; set; }
    public int CsvType { get; private set; }

    private bool IsActive { get; set; }
    private uint LifeTimeConsumedPercent { get; set; }

    public void OnAdd(LogicBattleModeServer battleMode)
    {
        BaseObject.OnAdd(battleMode);

        AreaEffect = (LogicAreaEffectData)ObjectData;

        if (AreaEffectType.Values.TryGetValue(AreaEffect.TypeInCsv, out var t))
            CsvType = t;

        LocalInit();
    }

    public void OnRemove()
    {
        BaseObject.OnRemove();
    }

    public void Tick()
    {
        BaseObject.Tick();
    }

    private void LocalInit()
    {
    }

    public bool ShouldEncodeFor(int encodeForIndex)
    {
        var v1 = BaseObject.ShouldEncodeFor(encodeForIndex);
        var v2 = true;

        return v1 && v2;
    }

    public void Encode(ref BitStream b, int encodeForIndex)
    {
        if (AreaEffect == null)
            return;

        BaseObject.Encode(ref b, encodeForIndex);

        if (CsvType == 8) // DelayedDamage
            b.WriteBoolean(IsActive);

        b.WritePositiveIntMax127(LifeTimeConsumedPercent);
    }
}