namespace ZOVserver.Services.Game.BattleService.Network;

public readonly struct SessionId(ulong low, ushort high) : IEquatable<SessionId>
{
    public ulong Low => low;
    public ushort High => high;

    public bool Equals(SessionId other)
    {
        return low == other.Low && high == other.High;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(low, high);
    }

    public static bool operator ==(SessionId left, SessionId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SessionId left, SessionId right)
    {
        return !left.Equals(right);
    }

    public override bool Equals(object? obj)
    {
        return obj is SessionId other && Equals(other);
    }

    public override string ToString()
    {
        return $"{high:X4}:{low:X16}";
    }
}