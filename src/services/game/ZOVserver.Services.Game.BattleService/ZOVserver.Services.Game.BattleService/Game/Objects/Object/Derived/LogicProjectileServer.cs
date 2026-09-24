using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;

public struct LogicProjectileServer : IGameObject
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

    public LogicProjectileData Projectile { get; set; }
    public int CsvRenderingType { get; private set; }

    public void OnAdd(LogicBattleModeServer battleMode)
    {
        BaseObject.OnAdd(battleMode);

        Projectile = (LogicProjectileData)ObjectData;

        if (ProjectileRenderingType.Values.TryGetValue(Projectile.Rendering, out var t))
            CsvRenderingType = t;

        LocalInit();
    }

    public void OnRemove()
    {
        BaseObject.OnRemove();
    }

    private void LocalInit()
    {
    }

    public void Tick()
    {
        BaseObject.Tick();
    }

    public bool ShouldEncodeFor(int encodeForIndex)
    {
        var v1 = BaseObject.ShouldEncodeFor(encodeForIndex);
        var v2 = true;

        return v1 && v2;
    }

    private uint State { get; set; }

    [SuppressMessage("ReSharper", "InvertIf")]
    public void Encode(ref BitStream b, int encodeForIndex)
    {
        if (Projectile == null)
            return;

        if (BaseObject.BattleMode == null)
            return;

        BaseObject.Encode(ref b, encodeForIndex);

        b.WritePositiveIntMax7(State);

        if (State != 4)
        {
            if (!Projectile.IsBouncing)
                goto LABEL_9;

            if (!b.WriteBoolean(false))
                goto LABEL_9;
        }

        if (BaseObject.BattleMode.TileMap.Width >= 22)
            b.WritePositiveIntMax4095(0);
        else
            b.WritePositiveIntMax1023(0);

        LABEL_9:
        b.WriteBoolean(false);

        if (Projectile.TriggerWithDelayMs == 0 &&
            Projectile.PreExplosionTimeMs == 0)
            goto LABEL_14;

        b.WritePositiveVIntMax65535(0);

        LABEL_14:
        if (Projectile.PreExplosionTimeMs != 0)
        {
            b.WritePositiveVIntMax65535(0);
            b.WritePositiveVIntMax65535(0);
        }

        b.WritePositiveIntMax1023(0);

        if (CsvRenderingType != 3)
            b.WritePositiveIntMax511(0);

        if (b.WriteBoolean(false))
        {
            b.WritePositiveVIntMax65535(0);
            b.WritePositiveVIntMax65535(0);
        }
    }
}