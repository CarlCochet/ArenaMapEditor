using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

public class OutputBitStream : IDisposable, IAsyncDisposable
{
    private Stream _stream;
    private readonly MemoryStream _memoryStream;
    private int _bitBuffer;
    private int _bitCursor;
    private bool _compressed;
    private long _offset;
    private readonly bool _isMemoryStream;

    public OutputBitStream(Stream stream)
    {
        _stream = stream;
    }

    public OutputBitStream()
    {
        _memoryStream = new MemoryStream();
        _stream = _memoryStream;
        _isMemoryStream = true;
    }

    public OutputBitStream(int capacity)
    {
        _memoryStream = new MemoryStream(capacity);
        _stream = _memoryStream;
        _isMemoryStream = true;
    }

    public byte[] GetData()
    {
        if (!_isMemoryStream)
            throw new InvalidOperationException("Use this method only with memory streams!");

        try
        {
            _stream.Close();
        }
        catch (IOException)
        {
            // Ignored
        }

        return _memoryStream.ToArray();
    }

    public static int GetFpBitsLength(double value)
    {
        if (value == 0.0)
        {
            return 1;
        }

        var longValue = (long)(value * 65536.0);
        return GetSignedBitsLength(longValue);
    }

    public long GetOffset()
    {
        return _offset;
    }

    public static int GetSignedBitsLength(long value)
    {
        int bitsLength;
        if (value == 0L)
        {
            bitsLength = 0;
        }
        else
        {
            bitsLength = (int)(Math.Floor(Math.Log(Math.Abs(value)) / Math.Log(2.0)) + 2.0);
        }

        return bitsLength;
    }

    public static int GetUnsignedBitsLength(long value)
    {
        return value < 1L ? 0 : (int)(Math.Floor(Math.Log(value) / Math.Log(2.0)) + 1.0);
    }

    public void Align()
    {
        if (_bitCursor <= 0)
        {
            return;
        }
        
        _stream.WriteByte((byte)_bitBuffer);
        _offset++;
        _bitCursor = 0;
        _bitBuffer = 0;
    }

    public void Close()
    {
        Align();
        _stream.Close();
    }

    public void EnableCompression()
    {
        if (_compressed)
        {
            return;
        }
        
        _stream = new DeflateStream(_stream, CompressionLevel.Optimal, false);
        _compressed = true;
    }

    public void Flush()
    {
        _stream.Flush();
    }

    public void WriteBooleanBit(bool value)
    {
        WriteUnsignedBits(value ? 1L : 0L, 1);
    }

    public void WriteBytes(sbyte[] buffer)
    {
        Align();
        if (buffer == null)
        {
            return;
        }
        
        foreach (var b in buffer)
        {
            _stream.WriteByte((byte)b);
        }
        _offset += buffer.Length;
    }

    public void WriteDouble(double value)
    {
        var longBits = BitConverter.DoubleToInt64Bits(value);
        var bytes = new sbyte[]
        {
            (sbyte)(longBits >> 32), (sbyte)(longBits >> 40), (sbyte)(longBits >> 48), (sbyte)(longBits >> 56),
            (sbyte)longBits, (sbyte)(longBits >> 8), (sbyte)(longBits >> 16), (sbyte)(longBits >> 24)
        };
        WriteBytes(bytes);
    }

    public void WriteFp16(double value)
    {
        WriteShort((short)(value * 256.0));
    }

    public void WriteDecimalFromBits(double value, int nBits)
    {
        var longValue = (long)(value * 65536.0);
        WriteSignedBits(longValue, nBits);
    }

    public void WriteFloat(float value)
    {
        WriteInt(BitConverter.SingleToInt32Bits(value));
    }

    public void WriteFloat16(float value)
    {
        var intBits = BitConverter.SingleToInt32Bits(value);
        var sign = Math.Abs((intBits & -2147483648) >> 31);
        var exponent = (intBits & 2139095040) >> 23;
        var mantissa = intBits & 8388607;
        var newExponent = 0;
        
        if (exponent != 0)
        {
            if (exponent == 255)
            {
                newExponent = 31;
            }
            else
            {
                newExponent = exponent - 127 + 15;
            }
        }

        var newMantissa = 0;
        switch (newExponent)
        {
            case < 0:
                newExponent = 0;
                break;
            case > 31:
                newExponent = 31;
                break;
            default:
                newMantissa = mantissa >> 13;
                break;
        }

        var result = sign << 15;
        result |= newExponent << 10;
        result |= newMantissa;
        WriteUnsignedShort(result);
    }

    public void WriteShort(short value)
    {
        Align();
        _stream.WriteByte((byte)(value & 255));
        _stream.WriteByte((byte)(value >> 8));
        _offset += 2L;
    }

    public void WriteInt(int value)
    {
        Align();
        _stream.WriteByte((byte)(value & 0xFF));
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)(value >> 16));
        _stream.WriteByte((byte)(value >> 24));
        _offset += 4L;
    }

    public void WriteLong(long value)
    {
        Align();
        _stream.WriteByte((byte)(value & 255L));
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)(value >> 16));
        _stream.WriteByte((byte)(value >> 24));
        _stream.WriteByte((byte)(value >> 32));
        _stream.WriteByte((byte)(value >> 40));
        _stream.WriteByte((byte)(value >> 48));
        _stream.WriteByte((byte)(value >> 56));
        _offset += 8L;
    }

    public void WriteByte(sbyte value)
    {
        Align();
        _stream.WriteByte((byte)value);
        _offset++;
    }

    public void WriteSignedBits(long value, int nBits)
    {
        var requiredBits = GetSignedBitsLength(value);
        if (nBits < requiredBits)
        {
            throw new IOException($"At least {requiredBits} bits needed for representation of {value}");
        }

        WriteInteger(value, nBits);
    }

    public void WriteString(string text)
    {
        WriteBytes(Array.ConvertAll(Encoding.UTF8.GetBytes(text), b => (sbyte)b));
        _stream.WriteByte(0);
        _offset++;
    }

    public void WriteUnsignedShort(int value)
    {
        Align();
        _stream.WriteByte((byte)(value & 0xFF));
        _stream.WriteByte((byte)(value >> 8));
        _offset += 2L;
    }

    public void WriteUnsignedInt(long value)
    {
        Align();
        _stream.WriteByte((byte)(value & 255L));
        _stream.WriteByte((byte)(value >> 8));
        _stream.WriteByte((byte)(value >> 16));
        _stream.WriteByte((byte)(value >> 24));
        _offset += 4L;
    }

    public void WriteUnsignedByte(short value)
    {
        Align();
        _stream.WriteByte((byte)value);
        _offset++;
    }

    public void WriteUnsignedBits(long value, int nBits)
    {
        var requiredBits = GetUnsignedBitsLength(value);
        if (nBits < requiredBits)
            throw new IOException($"At least {requiredBits} bits needed for representation of {value}. Used bits: {nBits}");

        WriteInteger(value, nBits);
    }

    private void WriteInteger(long value, int nBits)
    {
        for (var i = nBits; i > 0; i--)
        {
            _bitCursor++;
            if ((1L << (i - 1) & value) != 0L)
            {
                _bitBuffer = _bitBuffer | (1 << (8 - _bitCursor));
            }

            if (_bitCursor != 8)
            {
                continue;
            }
            
            _stream.WriteByte((byte)_bitBuffer);
            _offset++;
            _bitCursor = 0;
            _bitBuffer = 0;
        }
    }

    public void WriteBytes(sbyte[] buffer, int offset, int length)
    {
        Align();
        if (buffer == null)
        {
            return;
        }
        
        for (var i = 0; i < length; i++)
        {
            _stream.WriteByte((byte)buffer[offset + i]);
        }
        _offset += length;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _memoryStream?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
        {
            await _stream.DisposeAsync();
        }

        if (_memoryStream != null)
        {
            await _memoryStream.DisposeAsync();
        }
    }
}