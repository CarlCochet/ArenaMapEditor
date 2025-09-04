using Godot;
using System;
using System.IO;
using System.Text;

public class ExtendedDataInputStream
{
    public static readonly bool DefaultOrderingLittleEndian = true;
    private readonly byte[] _buffer;
    private int _position;
    private int _lastBooleanBitFieldPosition = -1;
    private sbyte _lastBooleanBitFieldIndex = -1;
    private sbyte _lastBooleanBitFieldValue;
    private readonly bool _littleEndian;

    protected ExtendedDataInputStream(byte[] buffer, bool littleEndian = true)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer), "Buffer can't be null");
        _littleEndian = littleEndian;
        _position = 0;
    }

    public ExtendedDataInputStream(Stream stream) : this(ReadFullStream(stream))
    {
    }

    public static ExtendedDataInputStream Wrap(byte[] data)
    {
        return new ExtendedDataInputStream(data);
    }

    public static ExtendedDataInputStream Wrap(byte[] data, bool littleEndian)
    {
        return new ExtendedDataInputStream(data, littleEndian);
    }

    public static ExtendedDataInputStream Wrap(Stream stream)
    {
        var buffer = ReadFullStream(stream);
        return new ExtendedDataInputStream(buffer);
    }

    public static ExtendedDataInputStream Wrap(Stream stream, bool littleEndian)
    {
        var buffer = ReadFullStream(stream);
        return new ExtendedDataInputStream(buffer, littleEndian);
    }

    private static byte[] ReadFullStream(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public bool LittleEndian => _littleEndian;

    public int Skip(int n)
    {
        if (n <= 0)
            return 0;

        int remaining = Available();
        int toSkip = Math.Min(remaining, n);
        _position += toSkip;
        return toSkip;
    }

    public int Available()
    {
        return _buffer.Length - _position;
    }

    public int ReadBytes(sbyte[] buffer, int offset, int size)
    {
        int available = Available();
        int toRead = Math.Min(available, Math.Min(buffer.Length - offset, size));
        
        for (int i = 0; i < toRead; i++)
        {
            buffer[offset + i] = (sbyte)_buffer[_position + i];
        }
        
        _position += toRead;
        return toRead;
    }

    public int ReadBytes(sbyte[] buffer)
    {
        int available = Available();
        int toRead = Math.Min(available, buffer.Length);
        
        for (int i = 0; i < toRead; i++)
        {
            buffer[i] = (sbyte)_buffer[_position + i];
        }
        
        _position += toRead;
        return toRead;
    }

    public sbyte[] ReadBytes(int length)
    {
        if (_position + length > _buffer.Length)
            throw new EndOfStreamException("Not enough data in buffer");

        var result = new sbyte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = (sbyte)_buffer[_position + i];
        }
        
        _position += length;
        return result;
    }

    public float ReadFloat()
    {
        if (_position + 4 > _buffer.Length)
            throw new EndOfStreamException("Not enough data for float");

        byte[] bytes = new byte[4];
        Array.Copy(_buffer, _position, bytes, 0, 4);
        
        if (_littleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
            
        _position += 4;
        return BitConverter.ToSingle(bytes, 0);
    }

    public short ReadShort()
    {
        if (_position + 2 > _buffer.Length)
            throw new EndOfStreamException("Not enough data for short");

        byte[] bytes = new byte[2];
        Array.Copy(_buffer, _position, bytes, 0, 2);
        
        if (_littleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
            
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
            throw new EndOfStreamException("Not enough data for int");

        byte[] bytes = new byte[4];
        Array.Copy(_buffer, _position, bytes, 0, 4);
        
        if (_littleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
            
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
            throw new EndOfStreamException("Not enough data for long");

        byte[] bytes = new byte[8];
        Array.Copy(_buffer, _position, bytes, 0, 8);
        
        if (_littleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
            
        _position += 8;
        return BitConverter.ToInt64(bytes, 0);
    }

    public sbyte ReadByte()
    {
        if (_position >= _buffer.Length)
            throw new EndOfStreamException("Not enough data for byte");
            
        return (sbyte)_buffer[_position++];
    }

    public short ReadUnsignedByte()
    {
        return (short)(ReadByte() & 0xFF);
    }

    public bool ReadBooleanBit()
    {
        int currentPosition = _position;
        
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
            int bit = _lastBooleanBitFieldValue & 0x80;
            return bit != 0;
        }
    }

    public string ReadString()
    {
        int startPosition = _position;
        int endPosition = startPosition;
        
        // Find null terminator
        while (endPosition < _buffer.Length && _buffer[endPosition] != 0)
        {
            endPosition++;
        }
        
        if (endPosition >= _buffer.Length)
            throw new EndOfStreamException("Unable to find a valid null terminated UTF-8 string end.");
            
        int length = endPosition - startPosition;
        
        if (length > 0)
        {
            byte[] stringBytes = new byte[length];
            Array.Copy(_buffer, _position, stringBytes, 0, length);
            _position = endPosition + 1; // Skip null terminator
            return Encoding.UTF8.GetString(stringBytes);
        }
        else
        {
            _position++; // Skip null terminator
            return string.Empty;
        }
    }

    public int Offset
    {
        get => _position;
        set => _position = value;
    }

    public int GetOffset()
    {
        return _position;
    }

    public void SetOffset(int offset)
    {
        if (offset < 0 || offset > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        _position = offset;
    }
}
