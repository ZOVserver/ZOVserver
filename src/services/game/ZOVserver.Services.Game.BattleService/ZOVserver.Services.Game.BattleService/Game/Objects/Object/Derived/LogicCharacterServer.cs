using System.Numerics;
using System.Runtime.CompilerServices;
using ZOVserver.Services.Game.BattleService.Game.Objects.Interfaces;
using ZOVserver.Services.Game.BattleService.Game.Objects.Object.Component;
using ZOVserver.Shared.Contracts.Laser.Combined.Player;
using ZOVserver.Shared.TitanRemnants.Helper.Enumerations.Game;
using ZOVserver.Shared.TitanRemnants.Mathem;
using ZOVserver.Shared.TitanRemnants.PAssets.Data;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;
using ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;
using ZOVserver.Shared.TitanRemnants.PAssets.Tables;
using ZOVserver.Shared.TitanRemnants.Streams;
using ZOVserver.Shared.TitanRemnants.Utility;

namespace ZOVserver.Services.Game.BattleService.Game.Objects.Object.Derived;

public struct LogicCharacterServer : IGameObject
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

    public LogicCharacterData Character { get; private set; }
    public int CsvType { get; private set; }

    public LogicPlayer? Player { get; set; }

    public LogicSkillServer[]? SkillServers;

    private Vector3 _targetPosition;
    private Vector3 _currentVelocity;
    private int _movementAngle;
    private bool _isMoving;

    public uint NowHitpoints { get; set; }
    public uint MaxHitpoints { get; set; }

    public void OnAdd(LogicBattleModeServer battleMode)
    {
        BaseObject.OnAdd(battleMode);

        Character = (LogicCharacterData)ObjectData;

        if (CharacterType.Values.TryGetValue(Character.TypeInCsv, out var t))
            CsvType = t;

        LocalInit();
    }

    public void OnRemove()
    {
        BaseObject.OnRemove();
    }

    private void LocalInit()
    {
        NowHitpoints = MaxHitpoints = (uint)Character.Hitpoints;

        if (Player != null)
            if (LogicGameModeUtil.HasTwoTeams(BaseObject.BattleMode?.State.GameMode ?? -1))
                _movementAngle = Player.PlayerIndex == 1 ? 90 : 270;

        var wData = LogicDataTables.GetDataByName<LogicSkillData>(Character.WeaponSkill);
        var uData = LogicDataTables.GetDataByName<LogicSkillData>(Character.UltimateSkill);

        var skillsCount = (wData != null ? 1 : 0) + (uData != null ? 1 : 0);

        // ReSharper disable once InvertIf
        if (skillsCount > 0)
        {
            SkillServers = new LogicSkillServer[skillsCount];

            var i = 0;

            if (wData != null)
                SkillServers[i++] = new LogicSkillServer(wData);

            if (uData != null)
                SkillServers[i] = new LogicSkillServer(uData);
        }
    }

    public bool IsPlayerControlRemoved()
    {
        return false;
    }

    public void MoveTo(int x, int y)
    {
        if (IsPlayerControlRemoved() || Character == null || BaseObject.BattleMode == null)
            return;

        _targetPosition = new Vector3(
            Math.Clamp(x, 50, BaseObject.BattleMode.TileMap.Width * 300 - 50),
            Math.Clamp(y, 50, BaseObject.BattleMode.TileMap.Height * 300 - 50),
            0
        );

        _isMoving = true;
    }

    public void Tick()
    {
        BaseObject.Tick();

        if (IsPlayerControlRemoved())
        {
            _isMoving = false;
            _currentVelocity = Vector3.Zero;
            return;
        }

        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (!_isMoving || Character == null || BaseObject.BattleMode == null)
            return;

        var diff = _targetPosition - BaseObject.Position;
        var distance = diff.Length();

        var speedPerTick = (Character.Speed + GetTotalSpeedBonus()) / 20.0f;
        var desiredVelocity = Vector3.Normalize(diff) * speedPerTick;

        _currentVelocity = Vector3.Lerp(_currentVelocity, desiredVelocity, 0.5f);

        var targetAngle = LogicMath.GetAngle(
            (int)(BaseObject.Position.X + _currentVelocity.X),
            (int)(BaseObject.Position.Y + _currentVelocity.Y),
            (int)BaseObject.Position.X,
            (int)BaseObject.Position.Y);

        var angleDiff = (targetAngle - _movementAngle + 540) % 360 - 180;
        _movementAngle = (_movementAngle + (int)(angleDiff * 0.8) + 360) % 360;

        if (distance <= _currentVelocity.Length())
        {
            BaseObject.Position = _targetPosition;
            _isMoving = false;
            _currentVelocity = Vector3.Zero;
        }
        else
        {
            BaseObject.Position += _currentVelocity;
        }
    }

    private int GetTotalSpeedBonus()
    {
        return 0;
    }

    public bool ShouldEncodeFor(int encodeForIndex)
    {
        var v1 = BaseObject.ShouldEncodeFor(encodeForIndex);
        var v2 = true;

        return v1 && v2;
    }

    public void Encode(ref BitStream b, int encodeForIndex)
    {
        if (Character == null)
            return;

        if (BaseObject.BattleMode == null)
            return;

        var gameMode = BaseObject.BattleMode.State.GameMode;

        BaseObject.Encode(ref b, encodeForIndex);

        var a3 = encodeForIndex == BaseObject.Index;

        if (Character.HasAutoAttack() || Character.Speed > 0 || Character.IsTrainingDummy())
        {
            if (a3)
            {
                if (b.WriteBoolean(IsPlayerControlRemoved()))
                {
                    b.WritePositiveIntMax511((uint)_movementAngle);
                    b.WritePositiveIntMax511((uint)_movementAngle);
                }
            }
            else
            {
                b.WritePositiveIntMax511((uint)_movementAngle);
                b.WritePositiveIntMax511((uint)_movementAngle);
            }

            b.WritePositiveIntMax7(0);
            b.WriteBoolean(false); // cocktail
            b.WriteIntMax63(0);

            b.WriteBoolean(false); // move blocked
            b.WriteBoolean(false); // stunned

            b.WriteBoolean(false); // unk

            b.WriteBoolean(false); // unk
        }
        else
        {
            b.WritePositiveIntMax7(0);

            if (Character.IsTrain())
            {
                b.WritePositiveIntMax511((uint)_movementAngle);
                b.WritePositiveIntMax511((uint)_movementAngle);
            }

            if (!string.IsNullOrWhiteSpace(Character.AreaEffect))
                b.WritePositiveIntMax511((uint)_movementAngle); // move angle
        }

        b.WritePositiveVIntMax255OftenZero(0); // HitEffectEnvp projectile + 1
        b.WritePositiveVIntMax255OftenZero(0); // HitEffect skin + 1

        b.WriteBoolean(false); // speed effect
        b.WriteBoolean(false); // slippery

        var tposion = b.WritePositiveIntMax3(0); // poison type
        if (tposion > 0)
            b.WriteBoolean(false); // unk (maybe slowdown)

        if (LogicGameModeUtil.PlayersCollectPowerCubes(gameMode))
        {
            if (Character.IsBoss())
            {
                b.WritePositiveIntMax2097151(NowHitpoints);
                b.WritePositiveIntMax2097151(MaxHitpoints);
            }
            else
            {
                b.WritePositiveVIntMax65535(NowHitpoints);
                b.WritePositiveVIntMax65535(MaxHitpoints);
            }
        }
        else if (LogicGameModeUtil.IsBigGameBoss(gameMode, false))
        {
            b.WritePositiveIntMax262143(NowHitpoints);
            b.WritePositiveIntMax262143(MaxHitpoints);
        }
        else if (Character.HasVeryMuchHitPoints())
        {
            b.WritePositiveIntMax524287(NowHitpoints);
            b.WritePositiveIntMax524287(MaxHitpoints);
        }
        else
        {
            b.WritePositiveIntMax8191(NowHitpoints);
            b.WritePositiveIntMax8191(MaxHitpoints);
        }

        if (Character.IsHero())
        {
            b.WritePositiveVIntMax255OftenZero(0); // objectives count

            if (LogicGameModeUtil.ModeHasCarryables(gameMode))
                if (b.WriteBoolean(false))
                {
                    b.WritePositiveVIntMax65535OftenZero(0);
                    b.WritePositiveVIntMax65535OftenZero(0);
                }

            b.WritePositiveVIntMax255OftenZero(0); // unk

            b.WriteBoolean(false); // big brawler
            var charging = b.WriteBoolean(false);
            b.WriteBoolean(false); // white shield
            b.WriteBoolean(false); // angry rage (maybe bull star power)
            b.WriteBoolean(false); // unk
            b.WriteBoolean(false); // ulti enabled
            b.WriteBoolean(false); // gold shield

            if (false) // LogicSkillData::getChargedShotCount(v42) >= 1  bea
                b.WriteIntMax3(0);

            if (Character.ShouldEncodePetStatus) // LogicCharacterData::shouldEncodePetStatus
                b.WriteBoolean(false);

            if (a3)
            {
                var c = b.WritePositiveIntMax15(0); // sound effect

                if (c >= 1)
                    b.WritePositiveIntMax7(0); // type
            }

            if (charging)
            {
                b.WritePositiveIntMax255(0);
                b.WritePositiveIntMax15(0);
            }

            switch (0) // chargeup type
            {
                case 5:
                {
                    b.WritePositiveIntMax2047(0);
                    break;
                }
                case 1:
                {
                    b.WritePositiveVIntMax255OftenZero(0);
                    break;
                }
                case > 0 when a3:
                {
                    b.WritePositiveIntMax1023(0);
                    break;
                }
            }
        }
        else if (Character.IsMinionDog())
        {
            b.WritePositiveIntMax31(0);
        }
        else if (Character.IsCarryable()) // LogicCharacterData::isCarryable
        {
            b.WritePositiveIntMax3(0);
            b.WritePositiveIntMax3(0);
        }
        else
        {
            if (false) // any skill BehaviorType is Charge
                if (b.WriteBoolean(false)) // IsCharging
                    b.WritePositiveIntMax15(0);

            if (Character.IsBase())
                b.WritePositiveIntMax127(0);
        }

        if (gameMode == 14 && Character.IsBoss())
            b.WriteBoolean(false);

        if (LogicGameModeUtil.IsBossWithDifferentStages(gameMode, Character.IsRoboWars()))
            b.WritePositiveIntMax7(0);

        b.WritePositiveIntMax3(0); // visibility state
        var v420 = b.WriteBoolean(false); // invisible effect
        b.WritePositiveIntMax511(0); // unk

        if (a3)
        {
            if (b.WriteBoolean(false)) // speed changed
                b.WriteIntMax1023(0); // new speed

            if (v420)
                b.WriteBoolean(false); // eye visor effect
        }

        var dc = b.WritePositiveIntMax31(0); // damage or heal count

        for (var i = 0; i < dc; i++)
            b.WriteIntMax32767(0); // damage or heal amount

        if (SkillServers == null)
            return;

        foreach (var skillServer in SkillServers)
            skillServer.Encode(ref b, encodeForIndex);
    }
}