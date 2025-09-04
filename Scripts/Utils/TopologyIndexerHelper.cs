using Godot;
using System;
using System.IO;

public class TopologyIndexerHelper
{
    public static int DataByLong(int nbBitsByData)
    {
        return sizeof(long) * 8 / nbBitsByData;
    }

    public static int DataByInt(int nbBitsByData)
    {
        return sizeof(int) * 8 / nbBitsByData;
    }

    public static int GetMask(int nbBits)
    {
        return (1 << nbBits) - 1;
    }

    public static int GetIndex(long[] indexes, int index, int tableSize)
    {
        var nbBits = ArenaMath.Log2I(tableSize);
        var dataCount = DataByLong(nbBits);
        var mask = (long)GetMask(nbBits);

        var i = indexes[index / dataCount];
        i >>= nbBits * (index % dataCount);
        return (int)(i & mask);
    }

    public static int GetIndex(int[] indexes, int index, int tableSize)
    {
        var nbBits = ArenaMath.Log2I(tableSize);
        var dataCount = DataByInt(nbBits);
        var mask = GetMask(nbBits);

        var i = indexes[index / dataCount];
        i >>= nbBits * (index % dataCount);
        return i & mask;
    }

    public static int[] CreateFor(int[] indexes, int count, ExtendedDataInputStream stream)
    {
        var result = new int[count];
        for (var i = 0; i < count; ++i)
        {
            result[i] = stream.ReadInt();
        }
        return result;
    }

    public static long[] CreateFor(long[] indexes, int count, ExtendedDataInputStream stream)
    {
        var result = new long[count];
        for (var i = 0; i < count; ++i)
        {
            result[i] = stream.ReadLong();
        }
        return result;
    }

}
