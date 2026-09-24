using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ZOVserver.Shared.TitanRemnants.Streams;

public sealed class ByteStream : IDisposable
{
    private readonly bool _expandable;
    private int _bitOffset;
    private byte[] _buffer;

    private bool _isPooledBuffer;

    public ByteStream(byte[] buffer)
    {
        _buffer = buffer;
        Length = buffer.Length;

        _isPooledBuffer = false;
    }

    public ByteStream(int capacity)
    {
        if (capacity > 0)
        {
            if (capacity > 32)
            {
                _buffer = ArrayPool<byte>.Shared.Rent(capacity);
                _buffer.AsSpan().Clear();
                _isPooledBuffer = true;
            }
            else
            {
                _buffer = new byte[capacity];
                _isPooledBuffer = false;
            }
        }
        else
        {
            _buffer = [];
            _isPooledBuffer = false;
        }

        Length = 0;
        _expandable = true;
    }

    public int Offset { get; private set; }

    public int Length { get; private set; }

    public void Dispose()
    {
        if (_isPooledBuffer && _buffer.Length > 0)
            ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = null!;

        _isPooledBuffer = false;

        Offset = 0;
        _bitOffset = 0;
        Length = 0;
    }

    public Span<byte> GetBuffer()
    {
        if (_buffer == null!)
            throw new NullReferenceException("_buffer is null!");

        return _buffer.AsSpan(0, Length);
    }

    public Memory<byte> GetMemory()
    {
        if (_buffer == null!)
            throw new NullReferenceException("_buffer is null!");

        return _buffer.AsMemory(0, Length);
    }

    public bool IsAtEnd()
    {
        return Offset >= Length;
    }

    public void SetOffset(int position)
    {
        Offset = position;
        _bitOffset = 0;
    }

    public void ResetOffset()
    {
        Offset = 0;
        _bitOffset = 0;
        Length = 0;
    }

    private void EnsureCapacity(int needed)
    {
        if (!_expandable) return;

        var required = Offset + needed;
        if (required <= _buffer.Length) return;

        var newSize = Math.Max(_buffer.Length * 2, required);

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        newBuffer.AsSpan().Clear();

        _buffer.AsSpan(0, Length).CopyTo(newBuffer.AsSpan(0, Length));

        if (_isPooledBuffer && _buffer != null!)
            ArrayPool<byte>.Shared.Return(_buffer);

        _buffer = newBuffer;
    }

    #region boolean

    public bool ReadBoolean()
    {
        if (_bitOffset == 0) Offset++;

        var value = (_buffer[Offset - 1] & (1 << _bitOffset)) != 0;
        _bitOffset = (_bitOffset + 1) & 7;
        return value;
    }

    public bool WriteBoolean(bool value)
    {
        if (_bitOffset == 0)
        {
            EnsureCapacity(1);
            Offset++;
        }

        if (value)
            _buffer[Offset - 1] |= (byte)(1 << _bitOffset);

        _bitOffset = (_bitOffset + 1) & 7;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region i8

    public sbyte ReadI8()
    {
        _bitOffset = 0;

        return (sbyte)_buffer[Offset++];
    }

    public sbyte WriteI8(sbyte value)
    {
        _bitOffset = 0;

        EnsureCapacity(1);

        _buffer[Offset] = (byte)value;

        Offset++;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region u8

    public byte ReadU8()
    {
        _bitOffset = 0;

        return _buffer[Offset++];
    }

    public byte WriteU8(byte value)
    {
        _bitOffset = 0;

        EnsureCapacity(1);

        _buffer[Offset] = value;

        Offset++;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region i16

    public short ReadI16(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadInt16BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(Offset));

        Offset += 2;
        return value;
    }

    public short WriteI16(short value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(2);

        if (bigEndian)
            BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 2;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region u16

    public ushort ReadU16(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(Offset));

        Offset += 2;
        return value;
    }

    public ushort WriteU16(ushort value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(2);

        if (bigEndian)
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 2;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region i32

    public int ReadI32(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadInt32BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(Offset));

        Offset += 4;
        return value;
    }

    public int WriteI32(int value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(4);

        if (bigEndian)
            BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 4;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region u32

    public uint ReadU32(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(Offset));

        Offset += 4;
        return value;
    }

    public uint WriteU32(uint value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(4);

        if (bigEndian)
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 4;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region i64

    public long ReadI64(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadInt64BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(Offset));

        Offset += 8;
        return value;
    }

    public long WriteI64(long value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(8);

        if (bigEndian)
            BinaryPrimitives.WriteInt64BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 8;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region u64

    public ulong ReadU64(bool bigEndian = true)
    {
        _bitOffset = 0;

        var value = bigEndian
            ? BinaryPrimitives.ReadUInt64BigEndian(_buffer.AsSpan(Offset))
            : BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(Offset));

        Offset += 8;
        return value;
    }

    public ulong WriteU64(ulong value, bool bigEndian = true)
    {
        _bitOffset = 0;

        EnsureCapacity(8);

        if (bigEndian)
            BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(Offset), value);
        else
            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(Offset), value);

        Offset += 8;
        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region i128

    public Int128 ReadI128()
    {
        _bitOffset = 0;

        Int128 result = 0;
        for (var i = 0; i < 4; i++)
            result |= (Int128)(uint)ReadI32() << (i * 32);

        return result;
    }

    public void WriteI128(Int128 value)
    {
        _bitOffset = 0;

        for (var i = 0; i < 4; i++)
            WriteI32((int)(value >> (i * 32)));
    }

    #endregion

    #region u128

    public UInt128 ReadU128()
    {
        _bitOffset = 0;

        UInt128 result = 0;
        for (var i = 0; i < 4; i++)
            result |= (UInt128)ReadU32() << (i * 32);

        return result;
    }

    public void WriteU128(UInt128 value)
    {
        _bitOffset = 0;

        for (var i = 0; i < 4; i++)
            WriteU32((uint)(value >> (i * 32)));
    }

    #endregion

    #region vint32

    public int ReadVInt32()
    {
        _bitOffset = 0;

        var value = 0;
        var byteValue = _buffer[Offset++];

        var v1 = (byteValue & 0x40) != 0;
        value |= byteValue & 0x3F;

        var startOffset = Offset - 1;
        while ((byteValue & 0x80) != 0)
            value |= ((byteValue = _buffer[Offset++]) & 0x7F) << (6 + 7 * (Offset - startOffset - 2));

        var v3 = Offset - startOffset;
        var shift = v3 is >= 1 and <= 4 ? 6 + 7 * (v3 - 1) : 31;
        return v1 ? (int)((uint)value | (0xFFFFFFFF << shift)) : value;
    }

    public int WriteVInt32(int value)
    {
        _bitOffset = 0;

        var v1 = value < 0;
        var v2 = v1 ? -value : value;
        var v3 = v2 < 0x40 ? 1 : v2 < 0x2000 ? 2 : v2 < 0x100000 ? 3 : v2 < 0x8000000 ? 4 : 5;

        var v4 = value <= -0x8000000;
        EnsureCapacity(v4 ? v3 = 5 : v3);

        _buffer[Offset++] = (byte)(((uint)value & 0x3F) | (uint)(v1 ? 0x40 : 0) | (uint)(v3 > 1 ? 0x80 : 0));
        for (int i = 1, shift = 6; i < v3 - (v4 ? 1 : 0); i++, shift += 7)
            _buffer[Offset++] = (byte)((((uint)value >> shift) & 0x7F) | (uint)(i < v3 - 1 ? 0x80 : 0));
        if (v4) _buffer[Offset++] = (byte)((value >> 27) & 0xF);

        if (Offset > Length) Length = Offset;
        return value;
    }

    #endregion

    #region vint64

    public long ReadVInt64()
    {
        var h = ReadVInt32();
        var l = ReadVInt32();

        return ((long)h << 32) | (uint)l;
    }

    public long WriteVInt64(long value)
    {
        WriteVInt32((int)(value >> 32));
        WriteVInt32((int)(value & 0xFFFFFFFF));

        return value;
    }

    #endregion

    #region string

    public string ReadString(int maxLength = 2048)
    {
        var length = ReadI32();

        if (length <= 0 || length > maxLength)
            return string.Empty;

        var result = Encoding.UTF8.GetString(_buffer, Offset, length);
        Offset += length;

        return result;
    }

    public void WriteString(string? value)
    {
        if (value == null)
        {
            WriteI32(-1);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteI32(byteCount);

        EnsureCapacity(byteCount);

        Offset += Encoding.UTF8.GetBytes(value, _buffer.AsSpan(Offset));
        if (Offset > Length) Length = Offset;
    }

    #endregion

    #region compressed_string

    public string ReadCompressedString()
    {
        var compressedLength = ReadI32();

        if (compressedLength <= 0)
            return string.Empty;

        var uncompressedLength = ReadI32(false);
        compressedLength -= 4;

        var compressedSpan = _buffer.AsSpan(Offset, compressedLength);
        Offset += compressedLength;

        try
        {
            using var resultStream = new MemoryStream();

            using var compressedStream = new MemoryStream(compressedSpan.ToArray());

            using (var zlib = new ZLibStream(compressedStream, CompressionMode.Decompress))
            {
                zlib.CopyTo(resultStream);
            }

            var uncompressedData = resultStream.ToArray();

            return uncompressedData.Length == uncompressedLength
                ? Encoding.UTF8.GetString(uncompressedData, 0, uncompressedData.Length)
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void WriteCompressedString(string value)
    {
        var uncompressedData = Encoding.UTF8.GetBytes(value);

        using var outputStream = new MemoryStream();

        using (var zlib = new ZLibStream(outputStream, CompressionLevel.Optimal, true))
        {
            zlib.Write(uncompressedData, 0, uncompressedData.Length);
        }

        var compressedData = outputStream.ToArray();

        WriteI32(compressedData.Length + 4);
        WriteI32(uncompressedData.Length, false);
        WriteBytesWithoutLength(compressedData);
    }

    #endregion

    #region bytes

    public Span<byte> ReadBytes()
    {
        _bitOffset = 0;

        var length = ReadI32();
        if (length <= 0 || Offset + length > Length)
            return Span<byte>.Empty;

        var result = _buffer.AsSpan(Offset, length);
        Offset += length;
        return result;
    }

    public int WriteBytes(byte[]? value)
    {
        _bitOffset = 0;

        if (value == null)
            return WriteI32(-1);

        var length = WriteI32(value.Length);
        WriteBytesWithoutLength(value);

        return length;
    }

    #endregion

    #region bytes_without_length

    public Span<byte> ReadBytesWithoutLength(int length)
    {
        _bitOffset = 0;

        if (length == -1)
            length = Length - Offset;

        if (length <= 0 || Offset + length > Length)
            return Span<byte>.Empty;

        var result = _buffer.AsSpan(Offset, length);
        Offset += length;
        return result;
    }

    public void WriteBytesWithoutLength(byte[]? value)
    {
        _bitOffset = 0;

        if (value == null || value.Length == 0) return;

        EnsureCapacity(value.Length);

        value.AsSpan().CopyTo(_buffer.AsSpan(Offset));
        Offset += value.Length;

        if (Offset > Length) Length = Offset;
    }

    public void WriteBytesWithoutLength(Span<byte> value)
    {
        _bitOffset = 0;

        if (value.Length == 0) return;

        EnsureCapacity(value.Length);

        value.CopyTo(_buffer.AsSpan(Offset));
        Offset += value.Length;

        if (Offset > Length) Length = Offset;
    }

    #endregion
}