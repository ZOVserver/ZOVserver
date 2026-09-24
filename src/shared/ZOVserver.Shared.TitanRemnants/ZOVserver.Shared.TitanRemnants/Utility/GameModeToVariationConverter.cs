namespace ZOVserver.Shared.TitanRemnants.Utility;

public static class GameModeToVariationConverter
{
    public static string GetGameModeVariation(string gameMode)
    {
        return gameMode switch
        {
            "CoinRush" => "GemGrab",
            "AttackDefend" => "Heist",
            "BountyHunter" => "Bounty",
            "LaserBall" => "BrawlBall",
            "BattleRoyale" => "Showdown",
            "BossFight" => "BigGame",
            "Survival" => "RoboRumble",
            "BattleRoyaleTeam" => "DuoShowdown",
            "Raid" => "BossFight",
            "RoboWars" => "Siege",
            "Training" => "Training",
            "BossRace" => "Takedown",
            "SoloBounty" => "LoneStar",
            "CaptureTheFlag" => "CTF",
            _ => "GemGrab"
        };
    }
}