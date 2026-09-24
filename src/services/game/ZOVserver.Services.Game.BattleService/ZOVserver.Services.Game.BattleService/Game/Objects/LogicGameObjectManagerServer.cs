using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;
using ZOVserver.Services.Game.BattleService.Game.Objects.Registry;
using ZOVserver.Services.Game.BattleService.Game.Objects.Registry.Id;
using ZOVserver.Shared.TitanRemnants.Helper.Battle;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.BattleService.Game.Objects;

public sealed class LogicGameObjectManagerServer
{
    private GameObjectRegistry<LogicAreaEffectServer> _areaEffects;
    private GameObjectRegistry<LogicCharacterServer> _characters;
    private GameObjectRegistry<LogicItemServer> _items;
    private GameObjectRegistry<LogicProjectileServer> _projectiles;

    public LogicGameObjectManagerServer(LogicBattleModeServer logicBattleModeServer)
    {
        BattleMode = logicBattleModeServer;

        const int capacity = 127;

        RegistryRunningId = new RegistryRunningId(capacity * 300);

        _areaEffects =
            new GameObjectRegistry<LogicAreaEffectServer>(BattleMode, GameObjectTypeHelperTable.AreaEffect,
                RegistryRunningId, capacity);
        _characters =
            new GameObjectRegistry<LogicCharacterServer>(BattleMode, GameObjectTypeHelperTable.Character,
                RegistryRunningId, capacity);
        _items =
            new GameObjectRegistry<LogicItemServer>(BattleMode, GameObjectTypeHelperTable.Item,
                RegistryRunningId, capacity);
        _projectiles =
            new GameObjectRegistry<LogicProjectileServer>(BattleMode, GameObjectTypeHelperTable.Projectile,
                RegistryRunningId, capacity);
    }

    public LogicBattleModeServer BattleMode { get; }

    public RegistryRunningId RegistryRunningId { get; }

    public void Encode(ref BitStream b, uint myObjectId, int encodeForIndex)
    {
        var gameMode = BattleMode.State.GameMode;
        var players = CollectionsMarshal.AsSpan(BattleMode.State.LogicPlayers);

        b.WritePositiveIntMax2097151(myObjectId);

        if (gameMode == 0)
            b.WritePositiveVIntMax65535((uint)BattleMode.CoinRushEndTimer);

        b.WriteBoolean(false);
        b.WriteIntMax15(BattleMode.GameOverState);

        BattleMode.TileMap.Encode(ref b);

        for (var i = 0; i < players.Length; i++)
        {
            ref var p = ref players[i];

            b.WriteBoolean(p.IsAlive);
            b.WriteBoolean(p.HasUlti());

            if (BattleMode.State.GameMode == 6)
                b.WritePositiveIntMax15(0);

            // ReSharper disable once InvertIf
            if (p.MyObjectIndex == encodeForIndex)
            {
                b.WritePositiveIntMax4095((uint)p.UltiCharge);

                b.WriteBoolean(false);
                b.WriteBoolean(false);
            }
        }

        var v57 = BattleMode.ShipAngle;

        if (BattleMode.ShipModifierEnabled)
            v57 += 90;

        b.WritePositiveVIntMax255OftenZero((uint)v57);

        switch (gameMode)
        {
            case 5:
            {
                b.WriteBoolean(true);
                b.WriteIntMax1(0);
                break;
            }
            case 6:
            {
                b.WritePositiveIntMax15(0);
                break;
            }
            case 9:
            {
                b.WritePositiveIntMax7(0);
                break;
            }
            case 16:
            {
                b.WritePositiveIntMax8191(0);
                b.WritePositiveIntMax8191(0);
                break;
            }
            default:
            {
                if (gameMode == 8 || LogicGameModeUtil.HasTwoBases(gameMode))
                {
                    b.WritePositiveIntMax127(0);

                    if (LogicGameModeUtil.HasTwoBases(gameMode))
                    {
                        b.WritePositiveIntMax127(0);

                        if (gameMode == 11)
                        {
                            b.WritePositiveIntMax255(0);
                            b.WritePositiveIntMax255(0);
                            b.WritePositiveIntMax7(0);
                            b.WritePositiveIntMax7(0);
                            b.WritePositiveIntMax63(0);
                            b.WritePositiveIntMax63(0);
                            b.WriteBoolean(false);
                        }
                    }
                    else
                    {
                        b.WritePositiveIntMax127(0);
                        b.WriteBoolean(true);
                    }
                }
                else
                {
                    switch (gameMode)
                    {
                        case 14:
                            b.WritePositiveIntMax127(0);
                            b.WritePositiveIntMax16383(0);
                            break;
                        case 13:
                            b.WritePositiveIntMax131071(0);
                            break;
                        case 10:
                            b.WritePositiveIntMax127(0);
                            break;
                        case 7:
                            b.WritePositiveIntMax127(0);
                            break;
                    }
                }

                break;
            }
        }

        for (var i = 0; i < players.Length; i++)
        {
            ref var p = ref players[i];

            if (b.WriteBoolean(!LogicGameModeUtil.IsBattleRoyale(gameMode)))
                switch (gameMode)
                {
                    case 15:
                        b.WritePositiveIntMax134217727((uint)p.GetScore());
                        break;
                    case 14:
                        b.WritePositiveIntMax524287((uint)p.GetScore());
                        break;
                    default:
                        b.WritePositiveVIntMax255((uint)p.GetScore());
                        break;
                }

            // ReSharper disable once InvertIf
            if (b.WriteBoolean(p.KillList.Count > 0))
            {
                b.WritePositiveIntMax15((uint)p.KillList.Count);

                foreach (var kill in p.KillList)
                {
                    b.WritePositiveIntMax15((uint)kill.PlayerIndex);
                    b.WriteIntMax7(kill.BountyStarsEarned);
                }
            }
        }

        var c = _characters.GetObjectsCount(encodeForIndex) +
                _projectiles.GetObjectsCount(encodeForIndex) +
                _areaEffects.GetObjectsCount(encodeForIndex) +
                _items.GetObjectsCount(encodeForIndex);

        b.WritePositiveIntMax127(c);

        _characters.EncodeDataGlobalIds(ref b, encodeForIndex);
        _projectiles.EncodeDataGlobalIds(ref b, encodeForIndex);
        _areaEffects.EncodeDataGlobalIds(ref b, encodeForIndex);
        _items.EncodeDataGlobalIds(ref b, encodeForIndex);

        _characters.EncodeObjectIds(ref b, encodeForIndex);
        _projectiles.EncodeObjectIds(ref b, encodeForIndex);
        _areaEffects.EncodeObjectIds(ref b, encodeForIndex);
        _items.EncodeObjectIds(ref b, encodeForIndex);

        _characters.EncodeObjects(ref b, encodeForIndex);
        _projectiles.EncodeObjects(ref b, encodeForIndex);
        _areaEffects.EncodeObjects(ref b, encodeForIndex);
        _items.EncodeObjects(ref b, encodeForIndex);

        if (gameMode != 7) return;

        // ReSharper disable once InvertIf
        if (b.WriteBoolean(true))
        {
            b.WritePositiveIntMax32767(0);
            b.WritePositiveIntMax65535(0);
        }
    }

    /// <returns>The generated Object ID, or <c>-1</c> if the object is invalid, or <c>-2</c> if the registry is disposed.</returns>
    public int AddLogicGameObject(in LogicCharacterServer logicCharacterServer)
    {
        return _characters.Add(in logicCharacterServer);
    }

    /// <returns>The generated Object ID, or <c>-1</c> if the object is invalid, or <c>-2</c> if the registry is disposed.</returns>
    public int AddLogicGameObject(in LogicProjectileServer logicProjectileServer)
    {
        return _projectiles.Add(in logicProjectileServer);
    }

    /// <returns>The generated Object ID, or <c>-1</c> if the object is invalid, or <c>-2</c> if the registry is disposed.</returns>
    public int AddLogicGameObject(in LogicAreaEffectServer logicAreaEffectServer)
    {
        return _areaEffects.Add(in logicAreaEffectServer);
    }

    /// <returns>The generated Object ID, or <c>-1</c> if the object is invalid, or <c>-2</c> if the registry is disposed.</returns>
    public int AddLogicGameObject(in LogicItemServer logicItemServer)
    {
        return _items.Add(in logicItemServer);
    }

    public ref T GetGameObjectById<T>(int id) where T : struct, IGameObject
    {
        if (!RunningIdToObjectTypeConverter.TryGetObjectType(id, out var type))
            return ref Unsafe.NullRef<T>();

        switch (type)
        {
            case GameObjectTypeHelperTable.Character:
            {
                if (typeof(T) != typeof(LogicCharacterServer))
                    return ref Unsafe.NullRef<T>();

                ref var character = ref _characters.GetById(id);

                if (Unsafe.IsNullRef(ref character))
                    return ref Unsafe.NullRef<T>();

                return ref Unsafe.As<LogicCharacterServer, T>(ref character);
            }
            case GameObjectTypeHelperTable.Projectile:
            {
                if (typeof(T) != typeof(LogicProjectileServer))
                    return ref Unsafe.NullRef<T>();

                ref var projectile = ref _projectiles.GetById(id);

                if (Unsafe.IsNullRef(ref projectile))
                    return ref Unsafe.NullRef<T>();

                return ref Unsafe.As<LogicProjectileServer, T>(ref projectile);
            }
            case GameObjectTypeHelperTable.AreaEffect:
            {
                if (typeof(T) != typeof(LogicAreaEffectServer))
                    return ref Unsafe.NullRef<T>();

                ref var areaEffect = ref _areaEffects.GetById(id);

                if (Unsafe.IsNullRef(ref areaEffect))
                    return ref Unsafe.NullRef<T>();

                return ref Unsafe.As<LogicAreaEffectServer, T>(ref areaEffect);
            }
            case GameObjectTypeHelperTable.Item:
            {
                if (typeof(T) != typeof(LogicItemServer))
                    return ref Unsafe.NullRef<T>();

                ref var item = ref _items.GetById(id);

                if (Unsafe.IsNullRef(ref item))
                    return ref Unsafe.NullRef<T>();

                return ref Unsafe.As<LogicItemServer, T>(ref item);
            }
            default:
                return ref Unsafe.NullRef<T>();
        }
    }

    public void RemoveGameObjectById(int id)
    {
        if (!RunningIdToObjectTypeConverter.TryGetObjectType(id, out var type))
            return;

        switch (type)
        {
            case GameObjectTypeHelperTable.Character:
            {
                ref var character = ref _characters.GetById(id);

                if (Unsafe.IsNullRef(ref character))
                    return;

                character.ShouldRemove = true;
                return;
            }
            case GameObjectTypeHelperTable.Projectile:
            {
                ref var projectile = ref _projectiles.GetById(id);

                if (Unsafe.IsNullRef(ref projectile))
                    return;

                projectile.ShouldRemove = true;
                return;
            }
            case GameObjectTypeHelperTable.AreaEffect:
            {
                ref var areaEffect = ref _areaEffects.GetById(id);

                if (Unsafe.IsNullRef(ref areaEffect))
                    return;

                areaEffect.ShouldRemove = true;
                return;
            }
            case GameObjectTypeHelperTable.Item:
            {
                ref var item = ref _items.GetById(id);

                if (Unsafe.IsNullRef(ref item))
                    return;

                item.ShouldRemove = true;
                return;
            }
        }
    }

    public Span<LogicCharacterServer> GetCharacters()
    {
        return _characters.All;
    }

    public Span<LogicProjectileServer> GetProjectiles()
    {
        return _projectiles.All;
    }

    public Span<LogicAreaEffectServer> GetAreaEffects()
    {
        return _areaEffects.All;
    }

    public Span<LogicItemServer> GetItems()
    {
        return _items.All;
    }

    public void Tick()
    {
        ObjectsTick();
        ObjectsSweep();
    }

    private void ObjectsTick()
    {
        _characters.Tick();
        _projectiles.Tick();
        _areaEffects.Tick();
        _items.Tick();
    }

    private void ObjectsSweep()
    {
        _characters.Sweep();
        _projectiles.Sweep();
        _areaEffects.Sweep();
        _items.Sweep();
    }
}