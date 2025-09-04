using Godot;
using System;

public class MapConstants
{
    private const int NumBitsCellXy = 18;
    private const int NumBitsCellZ = 10;
    public const int MapWidth = 18;
    public const int MapLength = 18;
    public const int NumCells = 324;
    public const int MaxElementsPerCell = 64;
    public const int CellXMax = 131071;
    public const int CellYMax = 131071;
    public const int CellZMax = 511;
    public const int CellXMin = -131072;
    public const int CellYMin = -131072;
    public const int CellZMin = -512;
    public const int MapXMax = 7281;
    public const int MapYMax = 7281;
    public const int MapZMax = 511;
    public const int MapXMin = -7281;
    public const int MapYMin = -7281;
    public const int MapZMin = -512;

    public static int GetMapCoordFromCellX(int cellX)
    {
        var x = cellX / MapConstants.MapWidth;
        if (cellX < 0 && cellX % MapConstants.MapWidth != 0)
        {
            --x;
        }
        return x;
    }

    public static int GetMapCoordFromCellY(int cellY)
    {
        var y = cellY / MapConstants.MapLength;
        if (cellY < 0 && cellY % MapConstants.MapLength != 0)
        {
            --y;
        }
        return y;
    }
}
