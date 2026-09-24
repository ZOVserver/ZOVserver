using System.Collections.Frozen;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;

public static class ItemNameType
{
    public static readonly FrozenDictionary<string, int> Values = new Dictionary<string, int>
    {
        { "Health", 0 },
        { "HealthPack", 0 },
        { "DuelistHealth", 0 },
        { "Speed", 1 },
        { "DamageAndSpeed", 2 },
        { "Point", 3 },
        { "Corpse", 4 },
        { "OrbSpawner", 5 },
        { "Mine", 6 },
        { "ControllerArchetypeMine", 6 },
        { "ClusterMine", 6 },
        { "ClusterOverchargedMine", 6 },
        { "SupplyCrate", 7 },
        { "BoxOfMines", 8 },
        { "BattleRoyaleBuff", 9 },
        { "Money", 10 },
        { "BoxOfBombs", 11 },
        { "Bomb", 12 },
        { "BoxOfSelfDestructBombs", 13 },
        { "SelfDestructBomb", 14 },
        { "Scrap", 15 },
        { "PetWars", 15 },
        { "SpringBoardLeft", 16 },
        { "SpringBoardLeft_Gale", 16 },
        { "SpringBoardRight", 17 },
        { "SpringBoardRight_Gale", 17 },
        { "SpringBoardUp", 18 },
        { "SpringBoardUp_Gale", 18 },
        { "SpringBoardDown", 19 },
        { "SpringBoardDown_Gale", 19 },
        { "SpringBoardUpLeft", 20 },
        { "SpringBoardUpLeft_Gale", 20 },
        { "SpringBoardUpRight", 21 },
        { "SpringBoardUpRight_Gale", 21 },
        { "SpringBoardDownLeft", 22 },
        { "SpringBoardDownLeft_Gale", 22 },
        { "SpringBoardDownRight", 23 },
        { "SpringBoardDownRight_Gale", 23 },
        { "TrainSpawner", 24 },
        { "ScorePole", 26 },
        { "ScoreFlag", 27 },
        { "ScoreFlagHoisted", 28 },
        { "GroundFiller", 29 },
        { "Teleport", 30 },
        { "OverchargedBoxOfBombs", 57 },
        { "OverchargedBomb", 58 },
        { "OverchargedMine", 61 },
        { "BoxOfOverchargedMines", 62 }
    }.ToFrozenDictionary();
}