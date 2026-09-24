using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.TitanRemnants.Mathem.Random;

public class LogicRandom
{
    private int _seed;

    public LogicRandom()
    {
    }

    public LogicRandom(int seed)
    {
        _seed = seed;
    }

    public int GetIteratedRandomSeed()
    {
        return _seed;
    }

    public void SetIteratedRandomSeed(int value)
    {
        _seed = value;
    }

    public int Rand(int max)
    {
        if (max <= 0) return 0;

        var seed = _seed;
        if (seed == 0) seed = -1;

        var tmp = seed ^ (seed << 13) ^ ((seed ^ (seed << 13)) >> 17);
        var tmp2 = tmp ^ (32 * tmp);
        _seed = tmp2;

        if (tmp2 < 0) tmp2 = -tmp2;
        return tmp2 % max;
    }

    public int IterateRandomSeed()
    {
        var seed = _seed;
        if (seed == 0) seed = -1;

        var tmp = seed ^ (seed << 13) ^ ((seed ^ (seed << 13)) >> 17);
        var tmp2 = tmp ^ (32 * tmp);

        return tmp2;
    }

    public int Decode(ByteStream byteStream)
    {
        return _seed = byteStream.ReadI32();
    }

    public void Encode(ByteStream byteStream)
    {
        byteStream.WriteI32(_seed);
    }
}