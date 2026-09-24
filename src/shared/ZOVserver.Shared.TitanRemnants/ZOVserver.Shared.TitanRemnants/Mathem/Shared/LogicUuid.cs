using ZOVserver.Shared.TitanRemnants.Streams;

namespace ZOVserver.Shared.TitanRemnants.Mathem.Shared;

public class LogicUuid
{
    private int _x;
    private int _y;

    public void CopyFrom(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public int Decode(ByteStream byteStream)
    {
        _x = byteStream.ReadVInt32();
        _y = byteStream.ReadVInt32();

        return _y;
    }

    public int Encode(ByteStream byteStream)
    {
        byteStream.WriteVInt32(_x);
        byteStream.WriteVInt32(_y);

        return _y;
    }

    public override string ToString()
    {
        return $"LogicUuid({_x}, {_y})";
    }
}