using ZOVserver.Shared.TitanRemnants.PAssets.Proto;

namespace ZOVserver.Shared.TitanRemnants.PAssets.Data.Realizations.L;

// Generated (-1) at 1778788288 (3d72cb81-b885-4ffb-9462-6602db688bb7).
public class LogicCampaignData(CsvElement csvElement) : LogicData(csvElement)
{
    public string Tid { get; } = csvElement.GetStringValue("TID");
    public string Location { get; } = csvElement.GetStringValue("Location");
    public string AllowedHeroes { get; } = csvElement.GetStringValue("AllowedHeroes");
    public string Reward { get; } = csvElement.GetStringValue("Reward");
    public int LevelGenerationSeed { get; } = csvElement.GetIntValue("LevelGenerationSeed");
    public string Map { get; } = csvElement.GetStringValue("Map");
    public string Enemies { get; } = csvElement.GetStringValue("Enemies");
    public int EnemyLevel { get; } = csvElement.GetIntValue("EnemyLevel");
    public string Boss { get; } = csvElement.GetStringValue("Boss");
    public int BossLevel { get; } = csvElement.GetIntValue("BossLevel");
    public string Base { get; } = csvElement.GetStringValue("Base");
    public int NumBases { get; } = csvElement.GetIntValue("NumBases");
    public int BaseLevel { get; } = csvElement.GetIntValue("BaseLevel");
    public string Tower { get; } = csvElement.GetStringValue("Tower");
    public int NumTowers { get; } = csvElement.GetIntValue("NumTowers");
    public int TowerLevel { get; } = csvElement.GetIntValue("TowerLevel");
    public int RequiredStars { get; } = csvElement.GetIntValue("RequiredStars");

    public override string ToString()
    {
        return $"""
                LogicCampaignData =>
                   Name = {Name},
                   TID = {Tid},
                   Location = {Location},
                   AllowedHeroes = {AllowedHeroes},
                   Reward = {Reward},
                   LevelGenerationSeed = {LevelGenerationSeed},
                   Map = {Map},
                   Enemies = {Enemies},
                   EnemyLevel = {EnemyLevel},
                   Boss = {Boss},
                   BossLevel = {BossLevel},
                   Base = {Base},
                   NumBases = {NumBases},
                   BaseLevel = {BaseLevel},
                   Tower = {Tower},
                   NumTowers = {NumTowers},
                   TowerLevel = {TowerLevel},
                   RequiredStars = {RequiredStars}
                """;
    }
}