using System.Numerics;
using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.TitanRemnants.Mathem.Vector;

public class LogicVector2(int x, int y)
{
    public int X = x;
    public int Y = y;

    public LogicVector2() : this(0, 0)
    {
        // pass.
    }

    public LogicVector2(Vector2 vector2) : this((int)vector2.X, (int)vector2.Y)
    {
        // pass.
    }

    public void Destruct()
    {
        X = 0;
        Y = 0;
    }

    public void Add(LogicVector2 vector2)
    {
        X += vector2.X;
        Y += vector2.Y;
    }

    public void Add(int x, int y)
    {
        X += x;
        Y += y;
    }

    public LogicVector2 Clone()
    {
        return new LogicVector2(X, Y);
    }

    public int Dot(LogicVector2 vector2)
    {
        return X * vector2.X + Y * vector2.Y;
    }

    public int GetAngle()
    {
        return LogicMath.GetAngle(X, Y);
    }

    public int GetAngleBetween(int x, int y)
    {
        return LogicMath.GetAngleBetween(LogicMath.GetAngle(X, Y), LogicMath.GetAngle(x, y));
    }

    public int GetDistance(LogicVector2 vector2)
    {
        var x = X - vector2.X;
        var distance = 0x7FFFFFFF;

        if ((uint)(x + 46340) > 92680) return LogicMath.Sqrt(distance);
        var y = Y - vector2.Y;

        if ((uint)(y + 46340) > 92680) return LogicMath.Sqrt(distance);
        var distanceX = x * x;
        var distanceY = y * y;

        if ((uint)distanceY < (distanceX ^ 0x7FFFFFFFu)) distance = distanceX + distanceY;

        return LogicMath.Sqrt(distance);
    }

    public int GetDistanceSquared(LogicVector2 vector2)
    {
        var x = X - vector2.X;
        var distance = 0x7FFFFFFF;

        if ((uint)(x + 46340) > 92680) return distance;
        var y = Y - vector2.Y;

        if ((uint)(y + 46340) > 92680) return distance;
        var distanceX = x * x;
        var distanceY = y * y;

        if ((uint)distanceY < (distanceX ^ 0x7FFFFFFFu)) distance = distanceX + distanceY;

        return distance;
    }

    public int GetDistanceSquaredTo(int x, int y)
    {
        var distance = 0x7FFFFFFF;

        x -= X;

        if ((uint)(x + 46340) > 92680) return distance;
        y -= Y;

        if ((uint)(y + 46340) > 92680) return distance;
        var distanceX = x * x;
        var distanceY = y * y;

        if ((uint)distanceY < (distanceX ^ 0x7FFFFFFFu)) distance = distanceX + distanceY;

        return distance;
    }

    public int GetLength()
    {
        var length = 0x7FFFFFFF;

        if ((uint)(46340 - X) > 92680) return LogicMath.Sqrt(length);
        if ((uint)(46340 - Y) > 92680) return LogicMath.Sqrt(length);

        var lengthX = X * X;
        var lengthY = Y * Y;

        if ((uint)lengthY < (lengthX ^ 0x7FFFFFFFu)) length = lengthX + lengthY;

        return LogicMath.Sqrt(length);
    }

    public int GetLengthSquared()
    {
        var length = 0x7FFFFFFF;

        if ((uint)(46340 - X) > 92680) return length;
        if ((uint)(46340 - Y) > 92680) return length;

        var lengthX = X * X;
        var lengthY = Y * Y;

        if ((uint)lengthY < (lengthX ^ 0x7FFFFFFFu)) length = lengthX + lengthY;

        return length;
    }

    public bool IsEqual(LogicVector2 vector2)
    {
        return X == vector2.X && Y == vector2.Y;
    }

    public bool IsInArea(int minX, int minY, int width, int height)
    {
        return X >= minX && X < minX + width &&
               Y >= minY && Y < minY + height;
    }

    public void Multiply(LogicVector2 vector2)
    {
        X *= vector2.X;
        Y *= vector2.Y;
    }

    public int Normalize(int value)
    {
        var length = GetLength();

        if (LogicMath.Abs(length) == 0)
            return length;

        X = X * value / length;
        Y = Y * value / length;

        return length;
    }

    public void Rotate(int degrees)
    {
        var newX = LogicMath.GetRotatedX(X, Y, degrees);
        var newY = LogicMath.GetRotatedY(X, Y, degrees);

        X = newX;
        Y = newY;
    }

    public void Set(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Set(LogicVector2 vector2)
    {
        X = vector2.X;
        Y = vector2.Y;
    }

    public void Set(Vector2 vector2)
    {
        X = (int)vector2.X;
        Y = (int)vector2.Y;
    }

    public void Substract(LogicVector2 vector2)
    {
        X -= vector2.X;
        Y -= vector2.Y;
    }

    public void Decode(ByteStream byteStream)
    {
        X = byteStream.ReadI32();
        Y = byteStream.ReadI32();
    }

    public void Encode(ByteStream byteStream)
    {
        byteStream.WriteI32(X);
        byteStream.WriteI32(Y);
    }

    public override string ToString()
    {
        return "LogicVector2(" + X + "," + Y + ")";
    }
}