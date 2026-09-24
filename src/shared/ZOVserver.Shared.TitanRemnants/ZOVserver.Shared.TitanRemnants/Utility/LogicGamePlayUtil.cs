using ZOVserver.Shared.TitanRemnants.Mathem;

namespace ZOVserver.Shared.TitanRemnants.Utility;

public static class LogicGamePlayUtil
{
    public static int GetDistanceBetween(int a1, int a2, int a3, int a4)
    {
        return LogicMath.Sqrt((a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2));
    }

    public static int GetDistanceSquaredBetween(int a1, int a2, int a3, int a4)
    {
        return (a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2);
    }

    public static float TweenCubicEaseIn(float a1, float a2, float a3)
    {
        return a2 + a1 * a1 * a1 * (a3 - a2);
    }

    public static float TweenCubicEaseOut(float a1, float a2, float a3)
    {
        return a2
               + (float)((float)(a1 + -1.0) * (float)(a1 + -1.0) * (float)(a1 + -1.0) + 1.0)
               * (a3 - a2);
    }

    public static int LineSegmentIntersectslineSegment(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8)
    {
        var v9 = a3 - a1;
        var v11 = (a8 - a6) * v9 - (a7 - a5) * (a4 - a2);
        var v12 = ((a7 - a5) * (a2 - a6) - (a8 - a6) * (a1 - a5)) / v11;
        var v13 = ((a2 - a6) * v9 - (a1 - a5) * (a4 - a2)) / v11;

        if (!(v12 <= 1.0) || !(v12 >= 0.0)) return 0;
        if (v13 >= 0.0 && v13 <= 1.0) return 1;
        return 0;
    }

    public static float RadToDeg(float a1)
    {
        return a1 * 57.296f;
    }

    public static float WeaponSpreadToAngleRad(int a1)
    {
        return (float)((float)a1 * 0.008);
    }

    public static bool IsJumpCharge(int chargeType)
    {
        var v1 = (uint)(chargeType - 2);

        if (v1 <= 9)
            return ((0x293u >> (int)v1) & 1) != 0;

        return false;
    }

    public static bool IsTargetedJumpCharge(int chargeType)
    {
        var v1 = (uint)(chargeType - 2);

        if (v1 <= 9)
            return ((0x213u >> (int)v1) & 1) != 0;

        return false;
    }

    public static int ScaleVectorTo(int a1, int a2, int a3)
    {
        var result = LogicMath.Sqrt(a1 * a1 + a2 * a2);
        {
            if (result > 0) result = a3 * a2 / result;
        }

        return result;
    }

    public static int RoundedDivision(int a1, int a2)
    {
        return (int)(float)((float)a1 / a2 + 0.5);
    }

    public static int LerpAngle(int a1, int a2, int a3)
    {
        var v5 = LogicMath.NormalizeAngle360(a1);
        var v6 = LogicMath.NormalizeAngle360(a2);

        if (v5 - v6 < 181)
        {
            if (v5 - v6 < -180)
                v5 += 360;
        }
        else
        {
            v5 -= 360;
        }

        var v7 = (int)((274877907L * (v6 * a3 + v5 * (1000 - a3))) >> 32);
        return LogicMath.NormalizeAngle360((int)((v7 >> 6) + ((uint)v7 >> 31)));
    }

    public static int GetPlayerCountWithGameModeVariation(int gameMode, bool friendlyRoom)
    {
        return gameMode switch
        {
            0 or 2 or 3 or 5 or 11 or 16 => friendlyRoom ? 3 * 2 : 3,
            6 or 14 or 15 => friendlyRoom ? 1 * 10 : 1,
            9 => friendlyRoom ? 2 * 5 : 2,
            7 => friendlyRoom ? 1 * 1 + 5 * 1 : 1,
            8 or 10 => 3 * 1,
            13 => 1 * 1,
            _ => -1
        };
    }

    public static int GetPlayerCountInTeamWithGameModeVariation(int gameMode, bool friendlyRoom)
    {
        return gameMode switch
        {
            0 or 2 or 3 or 5 or 11 or 16 => 3,
            6 or 14 or 15 => friendlyRoom ? 10 : 1,
            9 => 2,
            7 => 1,
            8 or 10 => 3,
            13 => 1,
            _ => -1
        };
    }

    public static int[] GetTeamCapacitiesWithGameModeVariation(int gameMode, int maxPlayersInTeam = 3,
        int maxPlayers = 6)
    {
        return gameMode switch
        {
            0 or 2 or 3 or 5 or 11 or 16 => [3, 3],
            8 or 10 => [3],
            13 => [1],
            6 or 14 or 15 => [1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
            9 => [2, 2, 2, 2, 2],
            7 => [5, 1],
            _ => Enumerable.Repeat(maxPlayersInTeam, maxPlayers / Math.Max(1, maxPlayersInTeam)).ToArray()
        };
    }
}