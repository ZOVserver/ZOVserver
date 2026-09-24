namespace ZOVserver.Shared.TitanRemnants.Mathem.Random;

public class LogicMersenneTwisterRandom
{
    private const int SeedCount = 624;

    private readonly int[] _mSeeds;
    private int _mIx;

    public LogicMersenneTwisterRandom() : this(324876476)
    {
    }

    public LogicMersenneTwisterRandom(int seed)
    {
        _mSeeds = new int[SeedCount];
        _mSeeds[0] = seed;

        for (var i = 1; i < SeedCount; i++)
        {
            seed = 1812433253 * (seed ^ (seed >> 30)) + 1812433253;
            _mSeeds[i] = seed;
        }
    }

    public int Rand(int max)
    {
        if (_mIx == 0)
            for (int i = 1, j = 0; i <= SeedCount; i++, j++)
            {
                var v4 = (_mSeeds[i % _mSeeds.Length] & 0x7fffffff) + (_mSeeds[j] & -0x80000000);
                var v6 = (v4 >> 1) ^ _mSeeds[(i + 396) % _mSeeds.Length];

                if ((v4 & 1) == 1) v6 ^= -0x66F74F21;

                _mSeeds[j] = v6;
            }

        var seed = _mSeeds[_mIx];
        _mIx = (_mIx + 1) % 624;

        seed ^= seed >> 11;
        seed = seed ^ ((seed << 7) & -1658038656) ^ (((seed ^ ((seed << 7) & -1658038656)) << 15) & -0x103A0000) ^
               ((seed ^ ((seed << 7) & -1658038656) ^ (((seed ^ ((seed << 7) & -1658038656)) << 15) & -0x103A0000)) >>
                18);
        var rnd = seed;

        if (rnd < 0) rnd = -rnd;

        return rnd % max;
    }
}