using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ZOVserver.Shared.TitanRemnants.Streams;

public ref struct BitStream : IDisposable
{
    private int _12;
    private int _4;

    private byte[]? _16R;
    private byte[]? _16W;

    public BitStream(byte[] buffer)
    {
        _16R = buffer;
        _16W = null;

        _12 = 0;
        _4 = 0;
    }

    public BitStream(int initialCapacity = 512)
    {
        _16R = null;
        _16W = ArrayPool<byte>.Shared.Rent(initialCapacity);

        if (_16W.Length > 0)
            _16W[0] = 0;

        _12 = 0;
        _4 = 0;
    }

    public void Dispose()
    {
        if (_16W != null)
            ArrayPool<byte>.Shared.Return(_16W);

        _16W = null;
        _16R = null;
    }

    #region Buffer

    public byte[] GetByteArray()
    {
        if (_16W == null)
            return _16R ?? [];

        var len = GetLength();

        var res = GC.AllocateUninitializedArray<byte>(len);
        _16W.AsSpan(0, len).CopyTo(res);

        return res;
    }

    public int GetLength()
    {
        return _12 > 0 ? _4 + 1 : _4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity()
    {
        var newSize = _16W!.Length << 1;
        var newArray = ArrayPool<byte>.Shared.Rent(newSize);

        Array.Copy(_16W, 0, newArray, 0, _16W.Length);
        Array.Clear(newArray, _16W.Length, newArray.Length - _16W.Length);

        ArrayPool<byte>.Shared.Return(_16W);
        _16W = newArray;
    }

    #endregion

    #region BasicPositive

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public uint ReadPositiveInt(byte bits)
    {
        if (_16R == null || _4 >= _16R.Length)
            return 0;

        var result = 0u;
        var br1 = 0;

        while (br1 < bits && _4 < _16R.Length)
        {
            var br2 = Math.Min(bits - br1, 8 - _12);
            result |= (uint)((_16R[_4] >> _12) & ((1 << br2) - 1)) << br1;

            _12 += br2;
            br1 += br2;

            // ReSharper disable once InvertIf
            if (_12 >= 8)
            {
                _12 = 0;
                _4++;
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public uint WritePositiveInt(uint value, byte bits)
    {
        if (_16W == null)
            return 0;

        var mask = (1u << bits) - 1;

        if (value > mask)
            value = mask;

        var bw1 = 0;

        while (bw1 < bits)
        {
            if (_4 >= _16W.Length)
                EnsureCapacity();

            var bw2 = Math.Min(bits - bw1, 8 - _12);
            _16W[_4] |= (byte)(((value >> bw1) & ((1u << bw2) - 1)) << _12);

            _12 += bw2;
            bw1 += bw2;

            // ReSharper disable once InvertIf
            if (_12 >= 8)
            {
                _12 = 0;

                if (++_4 < _16W.Length)
                    _16W[_4] = 0;
            }
        }

        return value;
    }

    #endregion

    #region BasicSigned

    public int ReadInt(byte bits)
    {
        var sign = ReadPositiveIntMax1() == 1 ? 1 : -1;
        var avalue = ReadPositiveInt(bits);

        return sign * (int)avalue;
    }

    public int WriteInt(int value, byte bits)
    {
        WritePositiveIntMax1((uint)(value >= 0 ? 1 : 0));
        WritePositiveInt((uint)Math.Abs(value), bits);

        return value;
    }

    #endregion

    #region BasicVarLen

    public uint ReadPositiveVInt(byte bits)
    {
        var v1 = (byte)(ReadPositiveInt(bits) + 1);

        return ReadPositiveInt(v1);
    }

    public uint WritePositiveVInt(uint value, byte bits)
    {
        var v1 = value == 0 ? 1 : BitOperations.Log2(value) + 1;

        WritePositiveInt((uint)(v1 - 1), bits);
        return WritePositiveInt(value, (byte)v1);
    }

    #endregion

    #region BasicTrueFalse

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean()
    {
        return ReadPositiveInt(1) == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WriteBoolean(bool value)
    {
        if (_16W == null)
            return value;

        if (_4 >= _16W.Length)
            EnsureCapacity();

        if (value)
            _16W[_4] |= (byte)(1 << _12);

        // ReSharper disable once InvertIf
        if (++_12 >= 8)
        {
            _12 = 0;

            if (++_4 < _16W.Length)
                _16W[_4] = 0;
        }

        return value;
    }

    #endregion

    #region ReadPositives

    public uint ReadPositiveIntMax1()
    {
        return ReadPositiveInt(1);
    }

    public uint ReadPositiveIntMax3()
    {
        return ReadPositiveInt(2);
    }

    public uint ReadPositiveIntMax7()
    {
        return ReadPositiveInt(3);
    }

    public uint ReadPositiveIntMax15()
    {
        return ReadPositiveInt(4);
    }

    public uint ReadPositiveIntMax31()
    {
        return ReadPositiveInt(5);
    }

    public uint ReadPositiveIntMax63()
    {
        return ReadPositiveInt(6);
    }

    public uint ReadPositiveIntMax127()
    {
        return ReadPositiveInt(7);
    }

    public uint ReadPositiveIntMax255()
    {
        return ReadPositiveInt(8);
    }

    public uint ReadPositiveIntMax511()
    {
        return ReadPositiveInt(9);
    }

    public uint ReadPositiveIntMax1023()
    {
        return ReadPositiveInt(10);
    }

    public uint ReadPositiveIntMax2047()
    {
        return ReadPositiveInt(11);
    }

    public uint ReadPositiveIntMax4095()
    {
        return ReadPositiveInt(12);
    }

    public uint ReadPositiveIntMax8191()
    {
        return ReadPositiveInt(13);
    }

    public uint ReadPositiveIntMax16383()
    {
        return ReadPositiveInt(14);
    }

    public uint ReadPositiveIntMax32767()
    {
        return ReadPositiveInt(15);
    }

    public uint ReadPositiveIntMax65535()
    {
        return ReadPositiveInt(16);
    }

    public uint ReadPositiveIntMax131071()
    {
        return ReadPositiveInt(17);
    }

    public uint ReadPositiveIntMax262143()
    {
        return ReadPositiveInt(18);
    }

    public uint ReadPositiveIntMax524287()
    {
        return ReadPositiveInt(19);
    }

    public uint ReadPositiveIntMax1048575()
    {
        return ReadPositiveInt(20);
    }

    public uint ReadPositiveIntMax2097151()
    {
        return ReadPositiveInt(21);
    }

    public uint ReadPositiveIntMax4194303()
    {
        return ReadPositiveInt(22);
    }

    public uint ReadPositiveIntMax8388607()
    {
        return ReadPositiveInt(23);
    }

    public uint ReadPositiveIntMax16777215()
    {
        return ReadPositiveInt(24);
    }

    public uint ReadPositiveIntMax33554431()
    {
        return ReadPositiveInt(25);
    }

    public uint ReadPositiveIntMax67108863()
    {
        return ReadPositiveInt(26);
    }

    public uint ReadPositiveIntMax134217727()
    {
        return ReadPositiveInt(27);
    }

    #endregion

    #region WritePositives

    public uint WritePositiveIntMax1(uint value)
    {
        return WritePositiveInt(value, 1);
    }

    public uint WritePositiveIntMax3(uint value)
    {
        return WritePositiveInt(value, 2);
    }

    public uint WritePositiveIntMax7(uint value)
    {
        return WritePositiveInt(value, 3);
    }

    public uint WritePositiveIntMax15(uint value)
    {
        return WritePositiveInt(value, 4);
    }

    public uint WritePositiveIntMax31(uint value)
    {
        return WritePositiveInt(value, 5);
    }

    public uint WritePositiveIntMax63(uint value)
    {
        return WritePositiveInt(value, 6);
    }

    public uint WritePositiveIntMax127(uint value)
    {
        return WritePositiveInt(value, 7);
    }

    public uint WritePositiveIntMax255(uint value)
    {
        return WritePositiveInt(value, 8);
    }

    public uint WritePositiveIntMax511(uint value)
    {
        return WritePositiveInt(value, 9);
    }

    public uint WritePositiveIntMax1023(uint value)
    {
        return WritePositiveInt(value, 10);
    }

    public uint WritePositiveIntMax2047(uint value)
    {
        return WritePositiveInt(value, 11);
    }

    public uint WritePositiveIntMax4095(uint value)
    {
        return WritePositiveInt(value, 12);
    }

    public uint WritePositiveIntMax8191(uint value)
    {
        return WritePositiveInt(value, 13);
    }

    public uint WritePositiveIntMax16383(uint value)
    {
        return WritePositiveInt(value, 14);
    }

    public uint WritePositiveIntMax32767(uint value)
    {
        return WritePositiveInt(value, 15);
    }

    public uint WritePositiveIntMax65535(uint value)
    {
        return WritePositiveInt(value, 16);
    }

    public uint WritePositiveIntMax131071(uint value)
    {
        return WritePositiveInt(value, 17);
    }

    public uint WritePositiveIntMax262143(uint value)
    {
        return WritePositiveInt(value, 18);
    }

    public uint WritePositiveIntMax524287(uint value)
    {
        return WritePositiveInt(value, 19);
    }

    public uint WritePositiveIntMax1048575(uint value)
    {
        return WritePositiveInt(value, 20);
    }

    public uint WritePositiveIntMax2097151(uint value)
    {
        return WritePositiveInt(value, 21);
    }

    public uint WritePositiveIntMax4194303(uint value)
    {
        return WritePositiveInt(value, 22);
    }

    public uint WritePositiveIntMax8388607(uint value)
    {
        return WritePositiveInt(value, 23);
    }

    public uint WritePositiveIntMax16777215(uint value)
    {
        return WritePositiveInt(value, 24);
    }

    public uint WritePositiveIntMax33554431(uint value)
    {
        return WritePositiveInt(value, 25);
    }

    public uint WritePositiveIntMax67108863(uint value)
    {
        return WritePositiveInt(value, 26);
    }

    public uint WritePositiveIntMax134217727(uint value)
    {
        return WritePositiveInt(value, 27);
    }

    #endregion

    #region ReadSigneds

    public int ReadIntMax1()
    {
        return ReadInt(1);
    }

    public int ReadIntMax3()
    {
        return ReadInt(2);
    }

    public int ReadIntMax7()
    {
        return ReadInt(3);
    }

    public int ReadIntMax15()
    {
        return ReadInt(4);
    }

    public int ReadIntMax31()
    {
        return ReadInt(5);
    }

    public int ReadIntMax63()
    {
        return ReadInt(6);
    }

    public int ReadIntMax127()
    {
        return ReadInt(7);
    }

    public int ReadIntMax255()
    {
        return ReadInt(8);
    }

    public int ReadIntMax511()
    {
        return ReadInt(9);
    }

    public int ReadIntMax1023()
    {
        return ReadInt(10);
    }

    public int ReadIntMax2047()
    {
        return ReadInt(11);
    }

    public int ReadIntMax4095()
    {
        return ReadInt(12);
    }

    public int ReadIntMax16383()
    {
        return ReadInt(14);
    }

    public int ReadIntMax32767()
    {
        return ReadInt(15);
    }

    public int ReadIntMax65535()
    {
        return ReadInt(16);
    }

    #endregion

    #region WriteSigneds

    public int WriteIntMax1(int value)
    {
        return WriteInt(value, 1);
    }

    public int WriteIntMax3(int value)
    {
        return WriteInt(value, 2);
    }

    public int WriteIntMax7(int value)
    {
        return WriteInt(value, 3);
    }

    public int WriteIntMax15(int value)
    {
        return WriteInt(value, 4);
    }

    public int WriteIntMax31(int value)
    {
        return WriteInt(value, 5);
    }

    public int WriteIntMax63(int value)
    {
        return WriteInt(value, 6);
    }

    public int WriteIntMax127(int value)
    {
        return WriteInt(value, 7);
    }

    public int WriteIntMax255(int value)
    {
        return WriteInt(value, 8);
    }

    public int WriteIntMax511(int value)
    {
        return WriteInt(value, 9);
    }

    public int WriteIntMax1023(int value)
    {
        return WriteInt(value, 10);
    }

    public int WriteIntMax2047(int value)
    {
        return WriteInt(value, 11);
    }

    public int WriteIntMax4095(int value)
    {
        return WriteInt(value, 12);
    }

    public int WriteIntMax16383(int value)
    {
        return WriteInt(value, 14);
    }

    public int WriteIntMax32767(int value)
    {
        return WriteInt(value, 15);
    }

    public int WriteIntMax65535(int value)
    {
        return WriteInt(value, 16);
    }

    #endregion

    #region ReadVInts

    public uint ReadPositiveVIntMax255()
    {
        return ReadPositiveVInt(3);
    }

    public uint ReadPositiveVIntMax65535()
    {
        return ReadPositiveVInt(4);
    }

    public uint ReadPositiveVIntMax2147483647()
    {
        return ReadPositiveVInt(5);
    }

    #endregion

    #region WriteVInts

    public uint WritePositiveVIntMax255(uint value)
    {
        return WritePositiveVInt(value, 3);
    }

    public uint WritePositiveVIntMax65535(uint value)
    {
        return WritePositiveVInt(value, 4);
    }

    public uint WritePositiveVIntMax2147483647(uint value)
    {
        return WritePositiveVInt(value, 5);
    }

    #endregion

    #region ReadVIntsOZ

    public uint ReadPositiveVIntMax255OftenZero()
    {
        return ReadPositiveInt(1) == 1 ? 0 : ReadPositiveVIntMax255();
    }

    public uint ReadPositiveVIntMax65535OftenZero()
    {
        return ReadPositiveInt(1) == 1 ? 0 : ReadPositiveVIntMax65535();
    }

    public uint ReadPositiveVIntMax2147483647OftenZero()
    {
        return ReadPositiveInt(1) == 1 ? 0 : ReadPositiveVIntMax2147483647();
    }

    #endregion

    #region WriteVIntsOZ

    public uint WritePositiveVIntMax255OftenZero(uint value)
    {
        if (value == 0)
        {
            WritePositiveIntMax1(1);
            return 0;
        }

        WritePositiveIntMax1(0);
        return WritePositiveVIntMax255(value);
    }

    public uint WritePositiveVIntMax65535OftenZero(uint value)
    {
        if (value == 0)
        {
            WritePositiveIntMax1(1);
            return 0;
        }

        WritePositiveIntMax1(0);
        return WritePositiveVIntMax65535(value);
    }

    public uint WritePositiveVIntMax2147483647OftenZero(uint value)
    {
        if (value == 0)
        {
            WritePositiveIntMax1(1);
            return 0;
        }

        WritePositiveIntMax1(0);
        return WritePositiveVIntMax2147483647(value);
    }

    #endregion
}