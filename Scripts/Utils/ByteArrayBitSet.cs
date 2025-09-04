using Godot;
using System;
using System.IO;

public class ByteArrayBitSet
{
    private const int BitsPerUnit = 8;
    private const int BitsPerUnitDivision = 3;
    private sbyte[] _bits;

    private ByteArrayBitSet()
    {
    }

    public ByteArrayBitSet(ByteArrayBitSet byteArrayBitSet)
    {
        _bits = new sbyte[byteArrayBitSet._bits.Length];
        Array.Copy(byteArrayBitSet._bits, 0, _bits, 0, _bits.Length);
    }

    public ByteArrayBitSet(int size)
    {
        _bits = new sbyte[size + 7 >> 3];
    }

    public ByteArrayBitSet(int size, bool defaultValue)
    {
        _bits = new sbyte[size + 7 >> 3];
        SetAll(defaultValue);
    }

    public bool Get(int bitIndex)
    {
        System.Diagnostics.Debug.Assert(bitIndex >> 3 < _bits.Length, 
            $"trying to get a bit index={bitIndex} but only {_bits.Length * 8} available.");

        var arrayIndex = bitIndex >> 3;
        var bitPosition = 7 - (bitIndex - (arrayIndex << 3));
        return (_bits[arrayIndex] & Bit(bitPosition)) != 0;
    }

    public void Set(int bitIndex, bool value)
    {
        System.Diagnostics.Debug.Assert(bitIndex >> 3 < _bits.Length, 
            $"trying to set a bit index={bitIndex} but only {_bits.Length * 8} available.");

        var arrayIndex = bitIndex >> 3;
        var bitPosition = 7 - (bitIndex - (arrayIndex << 3));
        if (value)
        {
            _bits[arrayIndex] = (sbyte)(_bits[arrayIndex] | Bit(bitPosition));
        }
        else
        {
            _bits[arrayIndex] = (sbyte)(_bits[arrayIndex] & ~Bit(bitPosition));
        }
    }

    public void SetAll(bool value)
    {
        if (value)
        {
            for (var i = 0; i < _bits.Length; i++)
            {
                _bits[i] = -1;
            }
        }
        else
        {
            for (var i = 0; i < _bits.Length; i++)
            {
                _bits[i] = 0;
            }
        }
    }

    private void Resize(int newSize)
    {
        System.Diagnostics.Debug.Assert(newSize >= _bits.Length * 8, 
            $"losing data in BitSet (oldSize={_bits.Length} newSize={newSize})");

        var newBits = new sbyte[(newSize + 7) / 8];
        Array.Copy(_bits, 0, newBits, 0, _bits.Length);
        _bits = newBits;
    }

    public int Capacity => _bits.Length * 8;

    private static sbyte Bit(int index)
    {
        System.Diagnostics.Debug.Assert(index < 8, $"bit index should be < 8, found: {index}");
        return (sbyte)(1 << index);
    }

    public sbyte[] GetByteArray()
    {
        return _bits;
    }

    public void Write(Stream outputStream)
    {
        var bytes = new byte[_bits.Length];
        for (var i = 0; i < _bits.Length; i++)
        {
            bytes[i] = (byte)_bits[i];
        }
        outputStream.Write(bytes, 0, bytes.Length);
    }

    public static ByteArrayBitSet FromByteArray(sbyte[] array, int offset, int size)
    {
        var bitSet = new ByteArrayBitSet();
        bitSet._bits = new sbyte[size];
        Array.Copy(array, offset, bitSet._bits, 0, size);
        return bitSet;
    }

    public static bool Get(sbyte[] bits, int index)
    {
        System.Diagnostics.Debug.Assert((index >> BitsPerUnitDivision) < bits.Length, 
            $"trying to get a bit index={index} but only {bits.Length * BitsPerUnit} available.");

        var unitPosition = index >> BitsPerUnitDivision;
        var bitPosition = 7 - (index - (unitPosition << BitsPerUnitDivision));
        return (bits[unitPosition] & Bit(bitPosition)) != 0;
    }

    public static int GetDataLength(int size)
    {
        return (size + (BitsPerUnit - 1)) >> BitsPerUnitDivision;
    }
}
