using System.Collections.Frozen;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Types;

public static class CharacterType
{
    public static readonly FrozenDictionary<string, int> Values = new Dictionary<string, int>
    {
        { "Hero", 0 },
        { "Npc_Boss", 1 },
        { "Minion_FollowOwner", 2 },
        { "Minion_FindEnemies", 3 },
        { "Minion_Building", 4 },
        { "Pvp_Base", 6 },
        { "Minion_Building_charges_ulti", 7 },
        { "Minion_Dog", 8 },
        { "LootBox", 10 },
        { "Minion_FindEnemies2", 11 },
        { "RoboWars", 12 },
        { "Train", 13 },
        { "Minion_Mirage", 15 },
        { "Npc_Boss_TownCrush", 16 },
        { "Carryable", 17 },
        { "Minion_Duplicate", 18 },
        { "Payload", 19 },
        { "Minion_Invasion", 20 },
        { "Minion_LastStand", 21 },
        { "Minion_Percenter", 22 },
        { "Minion_Twin", 23 },
        { "Minion_Critter", 24 },
        { "Minion_Orbiting", 25 },
        { "Her0", 26 },
        { "ArenaMinion", 27 },
        { "ArenaJungle", 28 },
        { "Decoration", 29 },
        { "Mega_Boss", 30 }
    }.ToFrozenDictionary();
}