using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;

public struct LogicItemServer : IGameObject
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

    public LogicItemData Item { get; set; }
    public int CsvNameType { get; private set; }

    private uint _spawningTick;
    private uint _spawnedTick;

    public void OnAdd(LogicBattleModeServer battleMode)
    {
        BaseObject.OnAdd(battleMode);

        Item = (LogicItemData)ObjectData;

        if (ItemNameType.Values.TryGetValue(Item.Name, out var t))
            CsvNameType = t;

        LocalInit();
    }

    public void OnRemove()
    {
        BaseObject.OnRemove();
    }

    private void LocalInit()
    {
        _spawningTick = 1;
        _spawnedTick = 0;
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

    [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
    public void Encode(ref BitStream b, int encodeForIndex)
    {
        if (Item == null)
            return;

        BaseObject.Encode(ref b, encodeForIndex);

        // OrbSpawner, SupplyCrate, BoxOfMines, BoxOfBombs, BoxOfSelfDestructBombs, TrainSpawner
        if (CsvNameType == 5 || (CsvNameType <= 13 && ((1 << CsvNameType) & 0x2980) != 0) || CsvNameType == 24)
        {
            b.WritePositiveIntMax16383(_spawningTick);
            b.WritePositiveIntMax16383(_spawnedTick);

            if (CsvNameType == 24)
                b.WritePositiveIntMax16383(0);
        }
        // Mine, Money, SelfDestructBomb, ScorePole
        else if (CsvNameType <= 25 && ((1 << CsvNameType) & 0x2005040) != 0)
        {
            b.WritePositiveIntMax63(0 / 50);
            b.WritePositiveIntMax63(0 / 50);
        }
        // SpringBoardLeft, SpringBoardRight, SpringBoardUp, SpringBoardDown, SpringBoardUpLeft, SpringBoardUpRight, SpringBoardDownLeft, SpringBoardDownRight
        else if ((CsvNameType & 0xFFFFFFF8) == 16)
        {
            b.WritePositiveIntMax3(0);
            b.WritePositiveIntMax63(0);
        }
    }
}