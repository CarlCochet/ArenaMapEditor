using Godot;
using System;

public static class Enums
{
    public enum FileDialogMode
    {
        Open = 0,
        Save = 1,
    }
    
    public enum Tool
    {
        None = 0,
        Select = 1,
        Brush = 2,
        Line = 3,
        Area = 4,
    }
    
    public enum Biome
    {
        Global = 0,
        Volcano = 1,
        Forest = 2,
        Mountain = 3,
        Castle = 4,
        Interior = 5,
        Snow = 6,
        Town = 7,
        Ruin = 8,
        Platform = 9,
        Swamp = 10,
    }

    public enum Category
    {
        Global = 0,
        Tile = 1,
        Plant = 2,
        Grass = 3,
        Tree = 4,
        Food = 5,
        Barrier = 6,
        Border = 7,
        Wall = 8,
        Roof = 9,
        Decoration = 10,
        Furniture = 11,
        Tool = 12,
        Statue = 13,
        Effect = 14,
        Water = 15,
    }

    public enum Mode
    {
        Gfx = 0,
        GfxCurrent = 1,
        Path = 2,
        Visibility = 3,
        Light = 4,
        Fight = 5,
    }

    public enum Direction
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4,
        UpLeft = 5,
        UpRight = 6,
        DownLeft = 7,
        DownRight = 8,
    }
}
