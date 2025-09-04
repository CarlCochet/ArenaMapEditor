using Godot;
using System;
using System.IO;
using System.Text;

public class ExtendedDataInputStream
{
    public static readonly bool DefaultLittleEndian = true;
    
    private readonly byte[] _buffer;
    private int _position;
    private bool _isLittleEndian;
    private int _lastBooleanBitFieldPosition = -1;
    private byte _lastBooleanBitFieldIndex = 255; // -1 as byte
    private byte _lastBooleanBitFieldValue;

    protected ExtendedDataInputStream(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentException("Buffer can't be null");
        _position = 0;
        _isLittleEndian = DefaultLittleEndian;
    }

    public ExtendedDataInputStream(Stream stream)
    {
        var buffer = ReadFullStream(stream);
        _buffer = buffer;
        _position = 0;
        _isLittleEndian = DefaultLittleEndian;
    }

    public static ExtendedDataInputStream Wrap(byte[] buffer)
    {
        return new ExtendedDataInputStream(buffer);
    }

    public static ExtendedDataInputStream Wrap(byte[] buffer, bool littleEndian)
    {
        var stream = new ExtendedDataInputStream(buffer) { _isLittleEndian = littleEndian };
        return stream;
    }

    public static ExtendedDataInputStream Wrap(Stream stream)
    {
        var buffer = ReadFullStream(stream);
        return new ExtendedDataInputStream(buffer);
    }

    public static ExtendedDataInputStream Wrap(Stream stream, bool littleEndian)
    {
        var buffer = ReadFullStream(stream);
        var dataStream = new ExtendedDataInputStream(buffer) { _isLittleEndian = littleEndian };
        return dataStream;
    }

    private static byte[] ReadFullStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public void Order(bool littleEndian)
    {
        _isLittleEndian = littleEndian;
    }

    public bool Order()
    {
        return _isLittleEndian;
    }

    public int Skip(int n)
    {
        if (n <= 0)
        {
            return 0;
        }
        
        int remaining = Available();
        int skipped = Math.Min(remaining, n);
        _position += skipped;
        return skipped;
    }

    public int Available()
    {
        return _buffer.Length - _position;
    }

    public int ReadBytes(byte[] b, int offset, int size)
    {
        var bytesToRead = Math.Min(Available(), Math.Min(b.Length - offset, size));
        Array.Copy(_buffer, _position, b, offset, bytesToRead);
        _position += bytesToRead;
        return bytesToRead;
    }

    public int ReadBytes(byte[] b)
    {
        var bytesToRead = Math.Min(Available(), b.Length);
        Array.Copy(_buffer, _position, b, 0, bytesToRead);
        _position += bytesToRead;
        return bytesToRead;
    }

    public byte[] ReadBytes(int length)
    {
        if (_position + length > _buffer.Length)
        {
            throw new EndOfStreamException("Not enough data in buffer");
        }
        
        var result = new byte[length];
        Array.Copy(_buffer, _position, result, 0, length);
        _position += length;
        return result;
    }

    public float ReadFloat()
    {
        if (_position + 4 > _buffer.Length)
        {
            throw new EndOfStreamException("Not enough data in buffer");
        }
        
        var bytes = new byte[4];
        Array.Copy(_buffer, _position, bytes, 0, 4);
        
        if (_isLittleEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        _position += 4;
        return BitConverter.ToSingle(bytes, 0);
    }

    public short ReadShort()
    {
        if (_position + 2 > _buffer.Length)
        {
            throw new EndOfStreamException("Not enough data in buffer");
        }
        
        var bytes = new byte[2];
        Array.Copy(_buffer, _position, bytes, 0, 2);
        
        if (_isLittleEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        _position += 2;
        return BitConverter.ToInt16(bytes, 0);
    }

    public int ReadUnsignedShort()
    {
        return ReadShort() & 0xFFFF;
    }

    public int ReadInt()
    {
        if (_position + 4 > _buffer.Length)
        {
            throw new EndOfStreamException("Not enough data in buffer");
        }
        
        var bytes = new byte[4];
        Array.Copy(_buffer, _position, bytes, 0, 4);
        
        if (_isLittleEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        _position += 4;
        return BitConverter.ToInt32(bytes, 0);
    }

    public long ReadUnsignedInt()
    {
        return ReadInt() & 0xFFFFFFFFL;
    }

    public long ReadLong()
    {
        if (_position + 8 > _buffer.Length)
        {
            throw new EndOfStreamException("Not enough data in buffer");
        }
        
        var bytes = new byte[8];
        Array.Copy(_buffer, _position, bytes, 0, 8);
        
        if (_isLittleEndian != BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        _position += 8;
        return BitConverter.ToInt64(bytes, 0);
    }

    public byte ReadByte()
    {
        return _position >= _buffer.Length 
            ? throw new EndOfStreamException("Not enough data in buffer")
            : _buffer[_position++];
    }

    public short ReadUnsignedByte()
    {
        return (short)(ReadByte() & 0xFF);
    }

    public bool ReadBooleanBit()
    {
        var currentPosition = _position;
        
        if (currentPosition == _lastBooleanBitFieldPosition && _lastBooleanBitFieldIndex <= 6)
        {
            _lastBooleanBitFieldIndex++;
            return (_lastBooleanBitFieldValue & (1 << (7 - _lastBooleanBitFieldIndex))) != 0;
        }
        else
        {
            _lastBooleanBitFieldIndex = 0;
            _lastBooleanBitFieldPosition = currentPosition + 1;
            _lastBooleanBitFieldValue = ReadByte();
            var firstBit = _lastBooleanBitFieldValue & 128;
            return firstBit != 0;
        }
    }

    public string ReadString()
    {
        var startPosition = _position;
        var endPosition = startPosition;
        
        while (endPosition < _buffer.Length && _buffer[endPosition] != 0)
        {
            endPosition++;
        }
        
        if (endPosition >= _buffer.Length)
        {
            throw new EndOfStreamException("Unable to find a valid Null terminated UTF-8 string end.");
        }
        
        var stringLength = endPosition - startPosition;
        if (stringLength > 0)
        {
            var stringBytes = new byte[stringLength];
            Array.Copy(_buffer, _position, stringBytes, 0, stringLength);
            _position = endPosition + 1; 
            return Encoding.UTF8.GetString(stringBytes);
        }
        _position++; 
        return string.Empty;
    }

    public int GetOffset()
    {
        return _position;
    }

    public void SetOffset(int offset)
    {
        if (offset < 0 || offset > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        _position = offset;
    }

}
