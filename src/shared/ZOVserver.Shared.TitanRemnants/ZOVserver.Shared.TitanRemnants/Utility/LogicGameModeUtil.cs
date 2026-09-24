using ZOVserver.Shared.TitanRemnants.Mathem;

namespace ZOVserver.Shared.TitanRemnants.Utility;

public static class LogicGameModeUtil
{
    public static bool HasTimerAndCanEndBeforeTimerRunsOut(int a1)
    {
        int result;
        {
            if (a1 <= 0x1D)
                result = (int)((0x3EEB09A5u >> a1) & 1);
            else
                result = 0;
        }

        return result > 0;
    }


    public static bool RoundResetsWhenObjectiveIsMissing(int a1)
    {
        var v1 = a1 - 5;

        int result;
        {
            if (v1 <= 0x12)
                result = (((int)18124960u + 1) >> v1) & 1;
            else
                result = 0;
        }

        return result > 0;
    }

    public static bool ModeHasCarryables(int a1)
    {
        return a1 is 5 or 16;
    }

    public static bool IsSpecialEvent(int a1)
    {
        return a1 == 7 || a1 == 18 || (a1 | 2) == 10;
    }

    public static bool IsSinglePlayerMode(int a1)
    {
        return a1 == 13;
    }

    public static bool IsSoloModeWithRespawns(int a1)
    {
        return (a1 | 1) == 15;
    }

    public static bool IsBattleRoyale(int a1)
    {
        return a1 is 6 or 9;
    }

    public static bool IsCoop(int a1)
    {
        bool result;

        if (a1 != 8)
            result = ((a1 - 10) & 0xFFFFFFF7) == 0;
        else result = true;

        return result;
    }

    public static bool HasTwoBases(int a1)
    {
        return a1 is 2 or 11;
    }

    public static bool HasTwoTeams(int a1)
    {
        return a1 is 0 or 2 or 3 or 5 or 7 or 11 or 16;
    }

    public static bool HasMoreThanTwoTeams(int a1)
    {
        a1 -= 6;

        bool result;
        {
            if (a1 <= 9)
                result = ((0x309u >> a1) & 1) != 0;
            else
                result = false;
        }

        return result;
    }

    public static bool HasSpawnProtectionInTheStart(int a1)
    {
        var v1 = a1 - 6;

        int result;
        {
            if (v1 <= 9)
                result = (int)((0x309u >> v1) & 1);
            else
                result = 0;
        }

        return result > 0;
    }

    public static bool HasOffScreenBossIndicator(int a1)
    {
        bool result;
        {
            if (a1 - 7 >= 2)
                result = a1 == 18 || (a1 | 4) == 14;
            else
                result = true;
        }

        return result;
    }

    public static bool IsBossWithDifferentStages(int gameMode, bool isSiegeRobot)
    {
        return gameMode switch
        {
            10 or 18 => true,
            11 when isSiegeRobot => true,
            _ => false
        };
    }

    public static bool HasBoxesWithPowerCubes(int a1)
    {
        return a1 is 6 or 9;
    }

    public static bool HasMultipleDifficulties(int a1)
    {
        return a1 == 18 || (a1 | 2) == 10;
    }

    public static bool HasAntiTeamingFeatures(int a1)
    {
        return a1 is 6 or 9;
    }

    public static bool PlayersDropPowerCubesOnDeath(int a1)
    {
        a1 -= 6;

        var result = false;
        {
            if (a1 <= 8)
                result = ((0x109u >> a1) & 1) != 0;
        }

        return result;
    }

    public static bool PlayersCollectPowerCubes(int a1)
    {
        a1 -= 6;

        if (a1 <= 8)
            return ((0x119 >> a1) & 1) != 0;

        return false;
    }

    public static bool PlayersCollectBountyStars(int a1)
    {
        return a1 is 3 or 15;
    }

    public static bool PlayersCollectBolts(int a1)
    {
        return a1 == 11;
    }

    public static bool DisplayOneHealthBar(int a1)
    {
        bool result;
        {
            if (a1 - 7 >= 2)
                result = a1 == 18 || (a1 | 4) == 14;
            else
                result = true;
        }

        return result;
    }

    public static bool HealthBarsFillUp(int a1)
    {
        return (a1 | 4) == 21;
    }

    public static bool HealthBarsAreInOneCorner(int a1)
    {
        return (a1 | 4) == 21;
    }

    public static bool SpectateFriendAfterDeath(int a1)
    {
        bool result;
        {
            if (a1 == 9)
                result = true;
            else
                result = a1 == 8 || a1 == 20 || ((a1 - 10) & 0xFFFFFFF7) == 0;
        }

        return result;
    }

    public static bool PreventWallsOnBase(int a1)
    {
        var v1 = a1 - 2;

        int result;
        {
            if (v1 <= 9)
                result = (int)((0x241u >> v1) & 1);
            else
                result = 0;
        }

        return result > 0;
    }

    public static bool MoveDeathNotificationsDown(int a1)
    {
        return a1 == 20 || a1 == 11 || (a1 | 4) == 21;
    }

    public static bool ModeHasSecondaryRespawnPoints(int a1)
    {
        return a1 is 5 or 23;
    }

    public static int GetNumberOfRounds(int a1)
    {
        return a1 == 20 ? 3 : 1;
    }

    public static int GetTilesPoisonedFromEdge(int a1, int a2)
    {
        if (a1 is 6 or 9) return LogicMath.Max(0, a2 - 440) / 20 / 5;
        return a1 == 20 ? 0 : LogicMath.Max(0, a2 - 900) / 20 / 2;
    }

    public static int GetPointInterval(int a1, int a2)
    {
        if (a1 == 21)
            return 10;
        if (a1 != 17)
            return 0;

        return a2 switch
        {
            2 => 16,
            1 => 12,
            _ => 20
        };
    }

    public static int GetRespawnSeconds(int a1)
    {
        return a1 switch
        {
            0 or 2 => 3,
            3 => 3,
            15 => 5,
            _ => 3
        };
    }

    public static bool IsTileOnPoisonArea(int a1, int a2, int a3, int a4)
    {
        if (a1 is < 555 or > 16383) return false;

        var v1 = (a1 - 555) / 100;
        return (a2 <= v1 || a2 >= 59 - v1 || a3 <= v1 || a3 >= 59 - v1) && a4 < 16383;
    }

    public static bool IsBigGameBoss(int a1, bool a2)
    {
        return a1 == 7 && a2;
    }

    public static int GetMmSectorByTrophies(int trophies)
    {
        return trophies switch
        {
            <= 500 => 1,
            < 1000 => 2,
            _ => 3
        };
    }

    public static (int min, int max) GetTrophyRangeByMmSector(int sector)
    {
        return sector switch
        {
            1 => (0, 500),
            2 => (501, 999),
            3 => (1000, int.MaxValue),
            _ => (0, 0)
        };
    }
}