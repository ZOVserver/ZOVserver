using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.TitanRemnants.Mathem.Shared;

public class LogicLong
{
    private int _highInteger;
    private int _lowInteger;

    public LogicLong()
    {
    }

    public LogicLong(int highInteger, int lowInteger)
    {
        _highInteger = highInteger;
        _lowInteger = lowInteger;
    }

    public LogicLong(long? value)
    {
        if (value == null) return;
        FromLong((long)value);
    }

    public void Set(int highInteger, int lowInteger)
    {
        _highInteger = highInteger;
        _lowInteger = lowInteger;
    }

    public long ToLong()
    {
        return ((long)_highInteger << 32) | (uint)_lowInteger;
    }

    public void FromLong(long value)
    {
        _highInteger = (int)(value >> 32);
        _lowInteger = (int)(value & 0xFFFFFFFF);
    }

    public int GetHigherInt()
    {
        return _highInteger;
    }

    public int GetLowerInt()
    {
        return _lowInteger;
    }

    public bool IsZero()
    {
        return _highInteger == 0 && _lowInteger == 0;
    }

    public int HashCode()
    {
        return _lowInteger + 31 * _highInteger;
    }

    public LogicLong Decode(ByteStream byteStream)
    {
        _highInteger = byteStream.ReadI32();
        _lowInteger = byteStream.ReadI32();

        return this;
    }

    public LogicLong Encode(ByteStream byteStream)
    {
        byteStream.WriteI32(_highInteger);
        byteStream.WriteI32(_lowInteger);

        return this;
    }

    public LogicLong Clone()
    {
        return new LogicLong(_highInteger, _lowInteger);
    }

    public override bool Equals(object? obj)
    {
        if (obj is LogicLong logicLong)
            return logicLong._highInteger == _highInteger && logicLong._lowInteger == _lowInteger;
        return false;
    }

    public static bool Equals(LogicLong? a1, LogicLong? a2)
    {
        if (a1 == null || a2 == null)
            return a1 == null && a2 == null;
        return a1._highInteger == a2._highInteger && a1._lowInteger == a2._lowInteger;
    }

    public static implicit operator LogicLong(long @long)
    {
        return new LogicLong((int)(@long >> 32), (int)@long);
    }

    public static implicit operator long(LogicLong @long)
    {
        return ((long)@long._highInteger << 32) | (uint)@long._lowInteger;
    }

    public override int GetHashCode()
    {
        return HashCode();
    }

    public override string ToString()
    {
        return $"LogicLong({_highInteger}, {_lowInteger})";
    }
}